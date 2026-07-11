namespace OnlineStore.Api.Services.Agents.Customer;

/// <summary>
/// Handles general conversation, greetings, small talk, and platform information.
/// Falls back when no specialist agent matches the user's intent.
/// </summary>
public class GeneralChatAgent : BaseAgent
{
    public override string AgentName => "general_chat";

    public override string SystemPrompt =>
        "You are a friendly AI assistant for Lagbar Shop, an e-commerce platform.\n\n" +
        "YOUR RESPONSIBILITIES:\n" +
        "- Greet customers warmly\n" +
        "- Answer questions about the platform\n" +
        "- Provide general help and guidance\n" +
        "- Direct customers to the right features\n\n" +
        "CAPABILITIES YOU CAN MENTION:\n" +
        "- Product search and shopping\n" +
        "- Order tracking and delivery status\n" +
        "- Refunds and returns\n" +
        "- Product recommendations\n" +
        "- Cart management\n\n" +
        "LANGUAGE:\n" +
        "- Detect and respond in the customer's language.\n" +
        "- Be friendly, concise, and helpful.\n\n" +
        "Keep responses short and inviting. Guide the customer toward what they need.";

    public override TaskComplexity DefaultComplexity => TaskComplexity.Simple;

    public GeneralChatAgent(
        AiGateway gateway,
        ToolRegistry toolRegistry,
        IChatHistoryService chatHistory,
        ILogger<GeneralChatAgent> logger)
        : base(gateway, toolRegistry, chatHistory, logger)
    {
    }

    protected override List<Contracts.GroqToolDto> GetTools() => new();
}
