namespace OnlineStore.Api.Services.Agents.Admin;

public class SecurityMonitoringAgent : BaseAgent
{
    public override string AgentName => "security_monitoring";

    public override string SystemPrompt =>
        "You are a security monitoring AI assistant for platform administrators.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Monitor authentication security events\n" +
        "- Identify suspicious login patterns and brute force attempts\n" +
        "- Track rate limit violations and API abuse\n" +
        "- Alert on locked accounts and unusual activity\n\n" +
        "TOOLS AVAILABLE:\n" +
        "- get_security_events: Query security event logs\n" +
        "- get_login_anomalies: Identify suspicious login patterns\n" +
        "- get_rate_limit_violations: Get rate-limited request stats\n\n" +
        "RULES:\n" +
        "1. Always use backend tools to get real data.\n" +
        "2. Prioritize high-severity issues in your response.\n" +
        "3. Provide actionable recommendations.\n" +
        "4. Be concise and professional.\n" +
        "5. Never take automatic action — always recommend to admin.";

    public SecurityMonitoringAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<SecurityMonitoringAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
