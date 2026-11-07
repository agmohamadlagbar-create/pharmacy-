using System.Text.Json;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents.ShopOwner;

/// <summary>
/// Routes shop owner requests to the appropriate specialist agent.
/// Similar to the Customer PlannerAgent but for shop owner intents.
/// </summary>
public class ShopOwnerPlannerAgent : BaseAgent
{
    public override string AgentName => "shop_planner";

    public override string SystemPrompt =>
        "You are an intent classification agent for shop owners on an e-commerce platform. " +
        "Your ONLY job is to classify the shop owner's intent into exactly ONE of these categories:\n\n" +
        "- \"product_management\" — creating products, editing descriptions, generating SEO keywords, translating, categorizing\n" +
        "- \"reporting\" — revenue reports, sales data, monthly/yearly summaries, data exports\n" +
        "- \"analytics\" — business insights, trend analysis, 'why are sales decreasing', performance explanations\n" +
        "- \"inventory\" — stock levels, restocking, fast-selling products, low stock alerts, inventory health, seasonal demand forecasting\n" +
        "- \"marketing\" — promotional campaigns, coupons, discounts, bundle suggestions, product promotions, marketing insights\n" +
        "- \"customer_insights\" — customer behavior, satisfaction analysis, top customers, reviews, complaints, customer segments\n" +
        "- \"business_advisor\" — long-term business strategy, business goals, advisor reports, health score, strategic recommendations tracking\n" +
        "- \"general_chat\" — greetings, platform questions, general help\n\n" +
        "RULES:\n" +
        "1. Respond ONLY with valid JSON. No extra text.\n" +
        "2. Format: {\"intent\": \"<category>\", \"tasks\": [\"<task1>\"], \"language\": \"en\"}\n" +
        "3. NEVER include explanations.\n\n" +
        "Examples:\n" +
        "User: 'Generate a description for my new product' → {\"intent\": \"product_management\", \"tasks\": [\"generate_product_description\"], \"language\": \"en\"}\n" +
        "User: 'Show my revenue this month' → {\"intent\": \"reporting\", \"tasks\": [\"generate_report\"], \"language\": \"en\"}\n" +
        "User: 'Why are my sales decreasing?' → {\"intent\": \"analytics\", \"tasks\": [\"analyze_sales_trend\"], \"language\": \"en\"}\n" +
        "User: 'What products need restocking?' → {\"intent\": \"inventory\", \"tasks\": [\"check_low_stock\"], \"language\": \"en\"}\n" +
        "User: 'Suggest a promotion for slow movers' → {\"intent\": \"marketing\", \"tasks\": [\"suggest_promotions\"], \"language\": \"en\"}\n" +
        "User: 'Who are my best customers?' → {\"intent\": \"customer_insights\", \"tasks\": [\"get_top_customers\"], \"language\": \"en\"}\n" +
        "User: 'Help me increase my revenue by 20% and grow my business' → {\"intent\": \"business_advisor\", \"tasks\": [\"manage_goals\"], \"language\": \"en\"}\n" +
        "User: 'Be my business advisor' → {\"intent\": \"business_advisor\", \"tasks\": [\"start_coaching\"], \"language\": \"en\"}\n" +
        "User: 'Help me improve my store' → {\"intent\": \"business_advisor\", \"tasks\": [\"inspect_store\"], \"language\": \"en\"}";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Complex;

    public ShopOwnerPlannerAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<ShopOwnerPlannerAgent> logger)
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
            "[ShopPlanner] Classified intent for shop owner {UserId}: {Intent}",
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
