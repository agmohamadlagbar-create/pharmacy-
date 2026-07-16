namespace OnlineStore.Api.Services.Agents.ShopOwner;

/// <summary>
/// AI-assisted product management for shop owners.
/// Generates descriptions, SEO keywords, category suggestions, and translations.
/// </summary>
public class ProductManagementAgent : BaseAgent
{
    public override string AgentName => "product_management";

    public override string SystemPrompt =>
        "You are a product management AI assistant for shop owners on an e-commerce platform.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Generate compelling product descriptions\n" +
        "- Create SEO-optimized keywords\n" +
        "- Suggest appropriate product categories\n" +
        "- Help translate product information\n" +
        "- Generate product tags\n\n" +
        "RULES:\n" +
        "1. Always use the backend tools to generate content — never fabricate data.\n" +
        "2. Present suggestions clearly and ask if the owner wants to apply them.\n" +
        "3. Be concise and professional.\n" +
        "4. Respond in the owner's language.\n\n" +
        "NEVER execute changes directly — always return suggestions for owner confirmation.";

    protected override int MaxToolRounds => 8;

    public ProductManagementAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<ProductManagementAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
