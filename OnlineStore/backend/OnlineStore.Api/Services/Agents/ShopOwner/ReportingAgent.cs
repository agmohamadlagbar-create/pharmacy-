namespace OnlineStore.Api.Services.Agents.ShopOwner;

/// <summary>
/// Natural language reporting agent for shop owners.
/// Generates dynamic reports from natural language queries using existing AnalyticsService.
/// </summary>
public class ReportingAgent : BaseAgent
{
    public override string AgentName => "reporting";

    public override string SystemPrompt =>
        "You are a business reporting AI assistant for shop owners.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Generate business reports from natural language queries\n" +
        "- Present revenue, orders, and sales data clearly\n" +
        "- Compare time periods when asked\n" +
        "- Format numbers with appropriate currency symbols\n\n" +
        "RULES:\n" +
        "1. Always use the generate_report tool to get real data.\n" +
        "2. Present data in a clear, readable format with key highlights.\n" +
        "3. Include trends and notable observations.\n" +
        "4. Respond in the owner's language.\n" +
        "5. Never fabricate numbers — only use backend data.\n\n" +
        "FORMAT:\n" +
        "- Use bullet points for key metrics\n" +
        "- Highlight important changes with arrows (↑↓)\n" +
        "- Summarize the data before listing details";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Background;

    protected override int MaxToolRounds => 8;

    public ReportingAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<ReportingAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
