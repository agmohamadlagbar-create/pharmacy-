using System.Diagnostics;
using System.Text;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// Abstract base class implementing common AI Gateway call logic for all agents.
/// Handles the tool-call loop, streaming, deduplication, and persistence.
/// </summary>
public abstract class BaseAgent : IAgent
{
    protected virtual int MaxToolRounds => 4;

    protected readonly AiGateway Gateway;
    protected readonly ToolRegistry ToolRegistry;
    protected readonly IChatHistoryService ChatHistory;
    protected readonly ILogger Logger;

    protected BaseAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger logger)
    {
        Gateway = gateway;
        ToolRegistry = toolRegistry;
        ChatHistory = chatHistory;
        Logger = logger;
    }

    public abstract string AgentName { get; }
    public abstract string SystemPrompt { get; }

    public virtual TaskComplexity DefaultComplexity => TaskComplexity.Medium;

    /// <summary>
    /// Optional A/B testing support. Override to return a variant system prompt
    /// instead of the default SystemPrompt. Returns null by default (no variant).
    /// The variant ID should be stored in the AgentContext for tracking.
    /// </summary>
    protected virtual string? GetSystemPromptVariant(AgentContext context) => context.SystemPromptVariant;

    /// <summary>
    /// Get the tool schemas this agent is authorized to use (from the ToolRegistry).
    /// </summary>
    protected virtual List<GroqToolDto> GetTools()
    {
        return ToolRegistry.GetToolsForAgent(AgentName);
    }

    public virtual async Task<AgentResult> ProcessAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        return await RunAgentLoopAsync(context, onToken: null, cancellationToken);
    }

    public virtual async Task<AgentResult> ProcessStreamingAsync(
        AgentContext context,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        return await RunAgentLoopAsync(context, onToken, cancellationToken);
    }

    /// <summary>
    /// Core agent execution loop: sends messages to Groq, handles tool calls,
    /// and loops until the model produces a final text response or max rounds are reached.
    /// </summary>
    protected virtual async Task<AgentResult> RunAgentLoopAsync(
        AgentContext context,
        Func<string, CancellationToken, Task>? onToken,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        Logger.LogInformation("[{AgentName}] Processing started for user {UserId}, conversation {ConversationId}",
            AgentName, context.UserId, context.ConversationId);

        // Build messages: system prompt (with optional A/B variant) + conversation history + user message
        var effectivePrompt = GetSystemPromptVariant(context) ?? SystemPrompt;
        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = effectivePrompt }
        };
        messages.AddRange(context.ConversationHistory);

        var tools = GetTools();
        var hasTools = tools.Count > 0;

        // If tools are available, add anti-hallucination instruction to the system prompt
        // and use low temperature for deterministic, grounded responses
        if (hasTools)
        {
            var toolNames = string.Join(", ", tools.Select(t => t.Function.Name));
            messages[0] = new GroqMessageDto
            {
                Role = "system",
                Content = effectivePrompt + "\n\n" +
                    "CRITICAL TOOL-USE RULES:\n" +
                    "- You have access to the following tools: " + toolNames + "\n" +
                    "- You MUST use these tools to perform any actions. NEVER claim to have performed an action without calling the appropriate tool.\n" +
                    "- If the user asks you to do something that requires a tool, call the tool if you have all required parameters. If you are missing required parameters (e.g. order ID), use available tools (e.g. get_recent_orders) to retrieve them, or ask the user to provide them first. Do NOT call a tool with placeholder/invalid parameters.\n" +
                    "- Only confirm success AFTER you receive a successful tool result.\n" +
                    "- If a tool returns an error or fails, report the error honestly to the user. Do NOT fabricate data or pretend the tool succeeded.\n" +
                    "- NEVER invent numbers, product names, prices, stock levels, revenue figures, or any other metrics. All data MUST come from tool results.\n" +
                    "- If you lack enough data from tools to answer a question, say so honestly instead of guessing."
            };
        }

        var fullResponse = new StringBuilder();
        var executedToolCalls = new HashSet<string>();
        string? structuredData = null;
        string? structuredDataType = null;

        for (var round = 0; round < MaxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // On the last round, inject a warning so the model doesn't try to call
            // tools that won't get executed
            var isLastRound = round == MaxToolRounds - 1;
            if (isLastRound && hasTools)
            {
                messages.Add(new GroqMessageDto
                {
                    Role = "system",
                    Content = "IMPORTANT: This is your FINAL opportunity to respond. You MUST provide your complete answer NOW based on the data you have already gathered from tools. Do NOT request any more tool calls — they will NOT be executed. Summarize your findings and respond to the user."
                });
            }

            var aiContext = new AiRequestContext
            {
                AgentName = AgentName,
                Complexity = DefaultComplexity,
                Temperature = hasTools ? 0.2 : null
            };

            ChatCompletionResult iterationResult;

            if (onToken is null)
            {
                iterationResult = await Gateway.SendAsync(
                    aiContext, messages, hasTools ? tools : null, cancellationToken);
            }
            else
            {
                iterationResult = await Gateway.SendStreamingAsync(
                    aiContext, messages, hasTools ? tools : null, onToken, cancellationToken);
            }

            if (!string.IsNullOrEmpty(iterationResult.Content))
            {
                if (fullResponse.Length > 0) fullResponse.Append("\n\n");
                fullResponse.Append(iterationResult.Content);
            }

            // No tool calls → break out of the loop and return the text response.
            // (e.g. answering support FAQs or asking for missing parameters like order ID).
            if (!iterationResult.ToolCalls.Any())
            {
                break;
            }

            // If this is the last round and the model still tried to call tools, ignore them
            if (isLastRound)
            {
                Logger.LogWarning("[{AgentName}] Model attempted tool calls on last round — ignoring. Tools: {ToolNames}",
                    AgentName, string.Join(", ", iterationResult.ToolCalls.Select(tc => tc.Function.Name)));
                break;
            }

            // Add assistant message with tool calls to context
            messages.Add(new GroqMessageDto
            {
                Role = "assistant",
                Content = string.IsNullOrWhiteSpace(iterationResult.Content) ? null : iterationResult.Content,
                ToolCalls = iterationResult.ToolCalls
            });

            // Execute each tool call
            foreach (var toolCall in iterationResult.ToolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Deduplication
                var toolKey = $"{toolCall.Function.Name}:{toolCall.Function.Arguments}";
                if (executedToolCalls.Contains(toolKey))
                {
                    Logger.LogWarning("[{AgentName}] Skipping duplicate tool call: {ToolKey}", AgentName, toolKey);
                    continue;
                }
                executedToolCalls.Add(toolKey);

                // Execute via registry with permission checking
                var toolStartedAt = DateTime.UtcNow;
                var toolResult = await ToolRegistry.ExecuteToolAsync(
                    toolCall.Function.Name,
                    toolCall.Function.Arguments,
                    context.UserId,
                    context.ConversationId,
                    AgentName,
                    context.UserRole);

                // Persist tool call
                try
                {
                    await ChatHistory.SaveToolCallAsync(
                        userId: context.UserId,
                        conversationId: context.ConversationId,
                        messageId: null,
                        toolCallId: toolCall.Id,
                        toolName: toolCall.Function.Name,
                        argumentsJson: toolCall.Function.Arguments,
                        resultJson: toolResult,
                        status: toolResult.StartsWith("Error") ? "Failed" : "Succeeded",
                        startedAtUtc: toolStartedAt,
                        completedAtUtc: DateTime.UtcNow,
                        errorMessage: toolResult.StartsWith("Error") ? toolResult : null);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[{AgentName}] Failed to persist tool call {ToolCallId}", AgentName, toolCall.Id);
                }

                // Emit system events for cart/recommendation updates
                if (onToken != null)
                {
                    await EmitSystemEventsAsync(toolCall.Function.Name, toolResult, onToken, cancellationToken);
                }

                // Track structured data from recommendations
                if (toolCall.Function.Name == "get_recommendations" && !toolResult.StartsWith("Error"))
                {
                    structuredData = toolResult;
                    structuredDataType = "recommendations";
                }

                // Build tool result content — inject explicit anti-hallucination context on errors
                var toolResultContent = toolResult;
                if (toolResult.StartsWith("Error"))
                {
                    toolResultContent = toolResult + "\n\n[SYSTEM: The tool call failed. Report this error to the user. Do NOT fabricate data or pretend this tool succeeded.]";
                }

                // Add tool result to message chain
                messages.Add(new GroqMessageDto
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Name = toolCall.Function.Name,
                    Content = toolResultContent
                });
            }
        }

        var responseText = fullResponse.ToString();
        if (string.IsNullOrWhiteSpace(responseText))
        {
            responseText = "I've processed your request. Is there anything else I can help you with?";
        }

        sw.Stop();
        Logger.LogInformation("[{AgentName}] Processing completed in {ElapsedMs}ms for user {UserId}",
            AgentName, sw.ElapsedMilliseconds, context.UserId);

        return new AgentResult
        {
            AgentName = AgentName,
            ResponseText = responseText,
            Success = true,
            RequiresReflection = ShouldReflect(context),
            StructuredData = structuredData,
            StructuredDataType = structuredDataType
        };
    }

    /// <summary>
    /// Override in subclasses to control when reflection is applied.
    /// Default: reflection for product recommendations and support resolutions.
    /// </summary>
    protected virtual bool ShouldReflect(AgentContext context) => false;

    /// <summary>
    /// Emit system events for cart updates and recommendations via the streaming callback.
    /// </summary>
    protected virtual async Task EmitSystemEventsAsync(
        string toolName,
        string toolResult,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken)
    {
        // Cart modification events
        if ((toolName is "add_to_cart" or "update_quantity" or "remove_from_cart") &&
            !toolResult.StartsWith("Error") &&
            toolResult.Contains("success", StringComparison.OrdinalIgnoreCase))
        {
            await onToken("[SYSTEM_EVENT:cart_updated]", cancellationToken);
        }

        // Recommendation events
        if (toolName == "get_recommendations" && !toolResult.StartsWith("Error"))
        {
            await onToken($"[SYSTEM_EVENT:recommendations:{toolResult}]", cancellationToken);
        }
    }
}
