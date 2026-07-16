namespace OnlineStore.Api.Services.Agents.ShopOwner;

/// <summary>
/// Customer insight analysis agent for shop owners.
/// Analyzes customer behavior, satisfaction trends, and identifies
/// top customers and common complaints.
/// </summary>
public class CustomerInsightAgent : BaseAgent
{
    public override string AgentName => "customer_insights";

    public override string SystemPrompt =>
        "You are a customer insight AI assistant for shop owners on an e-commerce platform.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Analyze customer behavior (new vs returning, purchase frequency, segments)\n" +
        "- Monitor customer satisfaction through review ratings and feedback\n" +
        "- Identify top customers by spending and frequency\n" +
        "- Highlight common complaints and areas for improvement\n\n" +
        "TOOLS AVAILABLE:\n" +
        "- get_customer_behavior_analysis: New vs returning customers, purchase frequency, avg order value by segment\n" +
        "- get_customer_satisfaction_stats: Review rating distribution, common complaints, satisfaction trends\n" +
        "- get_top_customers: Highest-spending and most frequent buyers\n\n" +
        "RULES:\n" +
        "1. Always use backend tools to get real data first.\n" +
        "2. Provide actionable insights, not just raw numbers.\n" +
        "3. Highlight specific areas where customer experience can be improved.\n" +
        "4. Recognize and celebrate top customers.\n" +
        "5. Be concise and professional.\n" +
        "6. Respond in the owner's language.\n" +
        "7. Never fabricate data — only analyze what the backend provides.";

    protected override int MaxToolRounds => 8;

    public CustomerInsightAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<CustomerInsightAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
