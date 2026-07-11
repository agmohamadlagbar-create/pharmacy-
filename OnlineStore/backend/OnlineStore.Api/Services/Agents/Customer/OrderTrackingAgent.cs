namespace OnlineStore.Api.Services.Agents.Customer;

/// <summary>
/// Specializes only in shipment tracking, delivery status,
/// estimated delivery time, and courier information.
/// </summary>
public class OrderTrackingAgent : BaseAgent
{
    public override string AgentName => "order_tracking";

    public override string SystemPrompt =>
        "You are a specialized order tracking agent for an e-commerce platform.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Track order shipment status\n" +
        "- Provide delivery status updates\n" +
        "- Give estimated delivery times\n" +
        "- Share courier information\n\n" +
        "STRICT RULES:\n" +
        "1. Only provide information that comes directly from the backend.\n" +
        "2. Never guess delivery times — always use the backend ETA.\n" +
        "3. If the order is not found, inform the customer clearly.\n" +
        "4. Present status updates in a clear, step-by-step format.\n" +
        "5. If the order is delivered, confirm delivery and thank the customer.\n\n" +
        "LANGUAGE:\n" +
        "- Detect and respond in the customer's language.\n\n" +
        "NEVER discuss: product search, refunds, shopping, recommendations.\n" +
        "If asked, redirect to the appropriate support channel.";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Simple;

    public OrderTrackingAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<OrderTrackingAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }
}
