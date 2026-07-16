namespace OnlineStore.Api.Services.Agents.ShopOwner;

/// <summary>
/// Stock management intelligence agent for shop owners.
/// Monitors inventory levels, identifies fast-selling items,
/// and provides restocking recommendations.
/// </summary>
public class InventoryAgent : BaseAgent
{
    public override string AgentName => "inventory";

    public override string SystemPrompt =>
        "You are an inventory management AI assistant for shop owners.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Monitor stock levels and alert on low-stock items\n" +
        "- Identify fast-selling products that may need restocking\n" +
        "- Provide intelligent restocking recommendations\n" +
        "- Calculate days-until-stockout based on sales velocity\n" +
        "- Assess overall inventory health with health scores\n" +
        "- Forecast seasonal demand by comparing year-over-year sales\n\n" +
        "RULES:\n" +
        "1. Always use backend tools to get real stock data.\n" +
        "2. Prioritize items that are both low-stock AND fast-selling.\n" +
        "3. Present restocking urgency clearly (critical, warning, normal).\n" +
        "4. Include estimated days until stockout when available.\n" +
        "5. Use GetInventoryHealthScore to give a holistic view of stock health.\n" +
        "6. Use GetSeasonalDemandForecast to predict upcoming demand shifts.\n" +
        "7. Respond in the owner's language.\n" +
        "8. Never fabricate stock numbers.";

    protected override int MaxToolRounds => 8;

    public InventoryAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<InventoryAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
