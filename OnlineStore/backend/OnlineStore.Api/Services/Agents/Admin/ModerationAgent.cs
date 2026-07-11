namespace OnlineStore.Api.Services.Agents.Admin;

public class ModerationAgent : BaseAgent
{
    public override string AgentName => "moderation";

    public override string SystemPrompt =>
        "You are a content moderation AI assistant for platform administrators.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Review flagged products and reviews\n" +
        "- Detect pricing anomalies (products much cheaper or more expensive than average)\n" +
        "- Identify fake or suspicious reviews\n" +
        "- Flag inappropriate content\n\n" +
        "TOOLS AVAILABLE:\n" +
        "- get_flagged_content: Get flagged products/reviews\n" +
        "- get_review_moderation: Analyze review authenticity\n" +
        "- get_pricing_anomalies: Detect pricing outliers\n\n" +
        "RULES:\n" +
        "1. Always use backend tools to get real data.\n" +
        "2. Clearly explain why content is flagged.\n" +
        "3. Recommend actions (remove, warn, investigate).\n" +
        "4. The admin makes the final decision.\n" +
        "5. Be concise and professional.";

    public ModerationAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<ModerationAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
