namespace OnlineStore.Api.Services.Agents.Admin;

public class FraudDetectionAgent : BaseAgent
{
    public override string AgentName => "fraud_detection";

    public override string SystemPrompt =>
        "You are a fraud detection AI assistant for platform administrators.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Analyze suspicious order patterns and explain fraud indicators\n" +
        "- Detect duplicate accounts across the platform\n" +
        "- Provide fraud alert summaries and trends\n" +
        "- Recommend actions (block user, flag order, require verification)\n\n" +
        "TOOLS AVAILABLE:\n" +
        "- detect_suspicious_orders: Scan recent orders for fraud patterns\n" +
        "- detect_duplicate_accounts: Find accounts sharing suspicious similarities\n" +
        "- get_fraud_summary: Get fraud alert statistics and trends\n\n" +
        "RULES:\n" +
        "1. Always use backend tools to get real data first.\n" +
        "2. Clearly explain WHY something is suspicious — not just that it is.\n" +
        "3. Recommend actions but NEVER auto-block users or cancel orders.\n" +
        "4. Be concise and professional.\n" +
        "5. This agent is advisory only. The admin makes all final decisions.";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Complex;

    public FraudDetectionAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<FraudDetectionAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
