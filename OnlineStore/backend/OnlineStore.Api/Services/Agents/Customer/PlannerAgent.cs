using System.Text.Json;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents.Customer;

/// <summary>
/// The orchestration brain for customer requests.
/// Classifies user intent and routes to the appropriate specialist agent.
/// The Planner does NOT call backend tools directly — it only coordinates.
/// </summary>
public class PlannerAgent : BaseAgent
{
    public override string AgentName => "planner";

    public override string SystemPrompt =>
        "You are an intent classification agent for an e-commerce platform. " +
        "Your ONLY job is to classify the user's intent into exactly ONE of these categories:\n\n" +
        "- \"shopping\" — product search, browsing, adding to cart, cart management, finding stores, comparing products\n" +
        "- \"support\" — order issues, refunds, returns, warranties, complaints, payment problems, FAQs\n" +
        "- \"recommendation\" — asking for product suggestions, deals, recommendations, 'what should I buy'\n" +
        "- \"order_tracking\" — tracking orders, delivery status, ETA, courier information, shipment updates\n" +
        "- \"general_chat\" — greetings, small talk, questions about the platform, anything that doesn't fit above\n\n" +
        "RULES:\n" +
        "1. Respond ONLY with valid JSON. No extra text before or after.\n" +
        "2. Format: {\"intent\": \"<category>\", \"tasks\": [\"<task1>\"], \"language\": \"en\", \"parameters\": {\"product_name\": \"<extracted product name or null>\", \"product_id\": <extracted product id as int or null>, \"quantity\": <extracted quantity as int or null>}}\n" +
        "3. The 'tasks' array should list the specific sub-tasks needed (e.g., [\"add_to_cart\", \"remove_from_cart\", \"update_quantity\", \"get_cart\", \"search_products\"])\n" +
        "4. If the intent is ambiguous, choose the MOST LIKELY intent.\n" +
        "5. Under 'parameters', extract 'product_name' (e.g. 'apple'), 'product_id' (if user specified a number like 'product 5'), and 'quantity' (e.g. 2, default to null if not specified).\n" +
        "6. NEVER include explanations, apologies, or conversation — ONLY the JSON object.\n\n" +
        "Examples:\n" +
        "User: 'I want to add 2 apples to my cart' → {\"intent\": \"shopping\", \"tasks\": [\"add_to_cart\"], \"language\": \"en\", \"parameters\": {\"product_name\": \"apple\", \"product_id\": null, \"quantity\": 2}}\n" +
        "User: 'remove product 12' → {\"intent\": \"shopping\", \"tasks\": [\"remove_from_cart\"], \"language\": \"en\", \"parameters\": {\"product_name\": null, \"product_id\": 12, \"quantity\": null}}\n" +
        "User: 'Where is my order #123?' → {\"intent\": \"order_tracking\", \"tasks\": [\"track_order\"], \"language\": \"en\", \"parameters\": null}\n" +
        "User: 'أريد إرجاع طلبي' → {\"intent\": \"support\", \"tasks\": [\"get_refund_status\"], \"language\": \"ar\", \"parameters\": null}\n" +
        "User: 'What do you recommend?' → {\"intent\": \"recommendation\", \"tasks\": [\"get_recommendations\"], \"language\": \"en\", \"parameters\": null}\n" +
        "User: 'Hello!' → {\"intent\": \"general_chat\", \"tasks\": [], \"language\": \"en\", \"parameters\": null}";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Complex;

    public PlannerAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<PlannerAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }

    /// <summary>
    /// The planner uses NO tools — it only classifies intent via LLM.
    /// </summary>
    protected override List<GroqToolDto> GetTools() => new();

    /// <summary>
    /// Classify the user's intent. Returns a structured plan as an AgentResult.
    /// The plan is stored in the result's StructuredData field as JSON.
    /// </summary>
    public override async Task<AgentResult> ProcessAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = SystemPrompt },
            new() { Role = "user", Content = context.UserMessage }
        };

        // Include last few messages for context (just role + content, not full history)
        if (context.ConversationHistory.Count > 0)
        {
            var recentContext = context.ConversationHistory
                .Where(m => m.Role is "user" or "assistant")
                .TakeLast(4)
                .ToList();

            // Insert context before the current user message
            messages.InsertRange(1, recentContext);
        }

        var aiContext = new AiRequestContext { AgentName = AgentName, Complexity = DefaultComplexity };
        var result = await Gateway.SendAsync(
            aiContext, messages, tools: null, cancellationToken);

        var plan = ParsePlan(result.Content);

        Logger.LogInformation(
            "[Planner] Classified intent for user {UserId}: {Intent} with tasks [{Tasks}]",
            context.UserId, plan.Intent, string.Join(", ", plan.Tasks));

        return new AgentResult
        {
            AgentName = AgentName,
            ResponseText = result.Content,
            Success = true,
            StructuredData = JsonSerializer.Serialize(plan),
            StructuredDataType = "plan"
        };
    }

    /// <summary>
    /// Planner does not support streaming — it produces a single classification.
    /// Falls back to non-streaming.
    /// </summary>
    public override Task<AgentResult> ProcessStreamingAsync(
        AgentContext context,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        return ProcessAsync(context, cancellationToken);
    }

    /// <summary>
    /// Parse the LLM's JSON response into a structured plan.
    /// Handles malformed responses gracefully.
    /// </summary>
    public static PlannerPlan ParsePlan(string llmResponse)
    {
        try
        {
            // Try to extract JSON from the response (handle cases where LLM adds extra text)
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = llmResponse[jsonStart..(jsonEnd + 1)];
                var plan = JsonSerializer.Deserialize<PlannerPlan>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (plan != null && !string.IsNullOrWhiteSpace(plan.Intent))
                    return plan;
            }
        }
        catch (JsonException)
        {
            // Fallback below
        }

        // Keyword-based fallback if LLM response is not valid JSON
        var lower = llmResponse.ToLowerInvariant();
        if (lower.Contains("shopping") || lower.Contains("cart") || lower.Contains("search") || lower.Contains("buy"))
            return new PlannerPlan { Intent = "shopping", Tasks = new List<string> { "search_products" } };
        if (lower.Contains("track") || lower.Contains("delivery") || lower.Contains("eta") || lower.Contains("shipment"))
            return new PlannerPlan { Intent = "order_tracking", Tasks = new List<string> { "track_order" } };
        if (lower.Contains("refund") || lower.Contains("return") || lower.Contains("support") || lower.Contains("complaint"))
            return new PlannerPlan { Intent = "support", Tasks = new List<string> { "get_order_details" } };
        if (lower.Contains("recommend") || lower.Contains("suggest"))
            return new PlannerPlan { Intent = "recommendation", Tasks = new List<string> { "get_recommendations" } };

        return new PlannerPlan { Intent = "general_chat", Tasks = new List<string>() };
    }
}

/// <summary>
/// Structured plan produced by the Planner agent.
/// </summary>
public class PlannerPlan
{
    public string Intent { get; set; } = "general_chat";
    public List<string> Tasks { get; set; } = new();
    public string Language { get; set; } = "en";
    public PlannerParameters? Parameters { get; set; }
}

public class PlannerParameters
{
    public string? ProductName { get; set; }
    public int? ProductId { get; set; }
    public int? Quantity { get; set; }
}
