using System.Text.Json;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents.Admin;

public class AdminPlannerAgent : BaseAgent
{
    public override string AgentName => "admin_planner";

    public override string SystemPrompt =>
        "You are an intent classification agent for platform administrators. " +
        "Your ONLY job is to classify the admin's intent into exactly ONE of these categories:\n\n" +
        "- \"fraud_detection\" — suspicious orders, duplicate accounts, payment fraud, refund abuse, order velocity\n" +
        "- \"security_monitoring\" — login failures, brute force, rate limits, suspicious API access, account lockouts\n" +
        "- \"platform_analytics\" — GMV, user growth, store rankings, category trends, regional metrics, platform overview\n" +
        "- \"store_approval\" — review store applications, risk assessment, compliance checks, store quality\n" +
        "- \"moderation\" — flagged products, inappropriate content, fake reviews, pricing anomalies, duplicate products\n" +
        "- \"global_support\" — escalated issues, cross-store disputes, user history, platform announcements\n" +
        "- \"general_chat\" — greetings, platform questions, general help\n\n" +
        "RULES:\n" +
        "1. Respond ONLY with valid JSON. No extra text.\n" +
        "2. Format: {\"intent\": \"<category>\", \"tasks\": [\"<task1>\"], \"language\": \"en\"}\n" +
        "3. NEVER include explanations.\n\n" +
        "Examples:\n" +
        "User: 'Show me suspicious orders' → {\"intent\": \"fraud_detection\", \"tasks\": [\"detect_suspicious_orders\"], \"language\": \"en\"}\n" +
        "User: 'Any security issues?' → {\"intent\": \"security_monitoring\", \"tasks\": [\"get_security_events\"], \"language\": \"en\"}\n" +
        "User: 'How is the platform performing?' → {\"intent\": \"platform_analytics\", \"tasks\": [\"get_platform_overview\"], \"language\": \"en\"}\n" +
        "User: 'Review pending stores' → {\"intent\": \"store_approval\", \"tasks\": [\"get_pending_stores\"], \"language\": \"en\"}\n" +
        "User: 'Flagged content' → {\"intent\": \"moderation\", \"tasks\": [\"get_flagged_content\"], \"language\": \"en\"}\n" +
        "User: 'Escalated issues' → {\"intent\": \"global_support\", \"tasks\": [\"get_escalated_issues\"], \"language\": \"en\"}";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Complex;

    public AdminPlannerAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<AdminPlannerAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }

    protected override List<GroqToolDto> GetTools() => new();

    public override async Task<AgentResult> ProcessAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<GroqMessageDto>
        {
            new() { Role = "system", Content = SystemPrompt },
            new() { Role = "user", Content = context.UserMessage }
        };

        var aiContext = new AiRequestContext { AgentName = AgentName, Complexity = DefaultComplexity };
        var result = await Gateway.SendAsync(
            aiContext, messages, tools: null, cancellationToken);

        var plan = Customer.PlannerAgent.ParsePlan(result.Content);

        Logger.LogInformation(
            "[AdminPlanner] Classified intent for admin {UserId}: {Intent}",
            context.UserId, plan.Intent);

        return new AgentResult
        {
            AgentName = AgentName,
            ResponseText = result.Content,
            Success = true,
            StructuredData = JsonSerializer.Serialize(plan),
            StructuredDataType = "plan"
        };
    }

    public override Task<AgentResult> ProcessStreamingAsync(
        AgentContext context,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        return ProcessAsync(context, cancellationToken);
    }
}
