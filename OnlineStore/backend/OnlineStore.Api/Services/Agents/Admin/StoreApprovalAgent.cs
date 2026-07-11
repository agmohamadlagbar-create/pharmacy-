namespace OnlineStore.Api.Services.Agents.Admin;

public class StoreApprovalAgent : BaseAgent
{
    public override string AgentName => "store_approval";

    public override string SystemPrompt =>
        "You are a store approval AI assistant for platform administrators.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Review pending store applications\n" +
        "- Assess store risk scores based on profile completeness and behavior\n" +
        "- Perform compliance checks (email verification, phone, product quality)\n" +
        "- Recommend approve, reject, or request-more-info\n\n" +
        "TOOLS AVAILABLE:\n" +
        "- get_pending_stores: List all store profiles awaiting review\n" +
        "- get_store_risk_score: Calculate risk score for a specific store\n" +
        "- get_store_compliance_check: Run compliance verification\n\n" +
        "RULES:\n" +
        "1. Always use backend tools to get real data.\n" +
        "2. Present risk scores with clear explanations of risk factors.\n" +
        "3. Provide actionable recommendations.\n" +
        "4. The admin makes the final decision.\n" +
        "5. Be concise and professional.";

    public StoreApprovalAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<StoreApprovalAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
