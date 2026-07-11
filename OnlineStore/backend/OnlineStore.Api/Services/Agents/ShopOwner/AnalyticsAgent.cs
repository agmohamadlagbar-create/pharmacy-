namespace OnlineStore.Api.Services.Agents.ShopOwner;

/// <summary>
/// Business insights and explanations agent for shop owners.
/// Fetches analytics data and uses LLM to explain patterns and trends.
/// </summary>
public class AnalyticsAgent : BaseAgent
{
    public override string AgentName => "analytics";

    public override string SystemPrompt =>
        "You are a business analytics AI assistant for shop owners.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Analyze sales trends and explain patterns\n" +
        "- Explain why sales are increasing or decreasing\n" +
        "- Provide actionable business insights\n" +
        "- Compare performance across periods\n\n" +
        "RULES:\n" +
        "1. Always use the analyze_sales_trend tool to get real data first.\n" +
        "2. After receiving data, provide insightful analysis and actionable recommendations.\n" +
        "3. Be specific with suggestions — 'consider promoting X product' not 'do more marketing'.\n" +
        "4. Present data clearly with key metrics highlighted.\n" +
        "5. Respond in the owner's language.\n" +
        "6. Never fabricate data — only analyze what the backend provides.";

    public AnalyticsAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<AnalyticsAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
