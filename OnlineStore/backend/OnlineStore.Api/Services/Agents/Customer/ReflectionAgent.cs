using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents.Customer;

/// <summary>
/// Metacognition layer — reviews and improves specialist agent responses
/// before they are delivered to the customer. Acts as a second layer of
/// reasoning to ensure completeness, clarity, and accuracy.
/// </summary>
public class ReflectionAgent : BaseAgent
{
    public override string AgentName => "reflection";

    public override string SystemPrompt =>
        "You are a quality review agent. You receive a draft response from another AI agent " +
        "and your job is to IMPROVE it before it reaches the customer.\n\n" +
        "REVIEW CHECKLIST:\n" +
        "1. COMPLETENESS — Does the response fully address the customer's question?\n" +
        "2. ACCURACY — Are all facts and data points correct? Is anything fabricated?\n" +
        "3. CLARITY — Is the response easy to understand? Is the language natural?\n" +
        "4. TONE — Is it friendly, professional, and empathetic?\n" +
        "5. MISSING INFO — Is any important information missing that the customer would need?\n" +
        "6. HALLUCINATION CHECK — Does the response claim anything that wasn't confirmed by backend data?\n" +
        "7. LANGUAGE MATCH — Is the response in the same language as the customer's message?\n\n" +
        "RULES:\n" +
        "1. If the draft is already high quality, return it AS-IS. Do NOT over-edit good responses.\n" +
        "2. If the draft needs improvement, rewrite it while keeping the same facts.\n" +
        "3. NEVER add information that wasn't in the original draft or backend results.\n" +
        "4. NEVER remove correct information from the draft.\n" +
        "5. Keep responses concise — remove unnecessary filler.\n" +
        "6. Preserve any special formatting (emoji, bullet points) if they help readability.\n" +
        "7. Output ONLY the improved response text. No meta-commentary about the review process.";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Background;

    public ReflectionAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<ReflectionAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }

    /// <summary>
    /// The reflection agent uses NO tools — it only reviews text.
    /// </summary>
    protected override List<GroqToolDto> GetTools() => new();

    /// <summary>
    /// Review and improve a specialist agent's draft response.
    /// </summary>
    public async Task<AgentResult> ReflectAsync(
        string userMessage,
        string draftResponse,
        string specialistAgentName,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = SystemPrompt },
            new()
            {
                Role = "user",
                Content = $"CUSTOMER MESSAGE:\n{userMessage}\n\n" +
                          $"DRAFT RESPONSE (from {specialistAgentName} agent):\n{draftResponse}\n\n" +
                          "Please review and return the improved response:"
            }
        };

        var aiContext = new AiRequestContext { AgentName = AgentName, Complexity = DefaultComplexity };
        var result = await Gateway.SendAsync(
            aiContext, messages, tools: null, cancellationToken);

        var improvedText = result.Content;

        // If the reflection agent returns empty, keep the original
        if (string.IsNullOrWhiteSpace(improvedText))
        {
            improvedText = draftResponse;
        }

        Logger.LogInformation(
            "[Reflection] Reviewed response from {SpecialistAgent}. Original length: {OriginalLen}, Improved length: {ImprovedLen}",
            specialistAgentName, draftResponse.Length, improvedText.Length);

        return new AgentResult
        {
            AgentName = AgentName,
            ResponseText = improvedText,
            Success = true
        };
    }

    /// <summary>
    /// Streaming reflection — emits improved response token by token.
    /// </summary>
    public async Task<AgentResult> ReflectStreamingAsync(
        string userMessage,
        string draftResponse,
        string specialistAgentName,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = SystemPrompt },
            new()
            {
                Role = "user",
                Content = $"CUSTOMER MESSAGE:\n{userMessage}\n\n" +
                          $"DRAFT RESPONSE (from {specialistAgentName} agent):\n{draftResponse}\n\n" +
                          "Please review and return the improved response:"
            }
        };

        var aiContext = new AiRequestContext { AgentName = AgentName, Complexity = DefaultComplexity };
        var result = await Gateway.SendStreamingAsync(
            aiContext, messages, tools: null, onToken, cancellationToken);

        var improvedText = result.Content;
        if (string.IsNullOrWhiteSpace(improvedText))
        {
            improvedText = draftResponse;
        }

        return new AgentResult
        {
            AgentName = AgentName,
            ResponseText = improvedText,
            Success = true
        };
    }
}
