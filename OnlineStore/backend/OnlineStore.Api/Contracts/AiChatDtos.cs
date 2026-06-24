using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Api.Contracts;

public class AiChatRequestDto
{
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional. Pass an existing conversation ID to continue that thread.</summary>
    public int? ConversationId { get; set; }
}

public class AiChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public int ConversationId { get; set; }
}

public static class AiChatStreamEventTypes
{
    public const string ConversationStarted = "conversation_started";
    public const string Token = "token";
    public const string Completed = "completed";
    public const string Error = "error";
    public const string CartUpdated = "cart_updated";
    public const string Recommendations = "recommendations";
}

public class AiChatStreamChunkDto
{
    public string EventType { get; set; } = AiChatStreamEventTypes.Token;
    public int ConversationId { get; set; }
    public string? Token { get; set; }
    public string? FullResponse { get; set; }
    public string? Error { get; set; }
}
