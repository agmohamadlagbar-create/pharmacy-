using OnlineStore.Domain.Entities;

namespace OnlineStore.Api.Services;

public interface IChatHistoryService
{
    /// <summary>Create a new conversation for the authenticated user.</summary>
    Task<AiConversation> CreateConversationAsync(string userId);

    /// <summary>Get a conversation if it belongs to the authenticated user.</summary>
    Task<AiConversation?> GetConversationAsync(string userId, int conversationId);

    /// <summary>Load the last N messages for a user-owned conversation as Groq-compatible DTOs.</summary>
    Task<List<Contracts.GroqMessageDto>> LoadRecentMessagesAsync(string userId, int conversationId, int count = 20);

    /// <summary>Persist a user or assistant message to a user-owned conversation.</summary>
    Task SaveMessageAsync(string userId, int conversationId, string role, string content);

    /// <summary>Persist a tool-call execution record to a user-owned conversation.</summary>
    Task SaveToolCallAsync(
        string userId,
        int conversationId,
        int? messageId,
        string toolCallId,
        string toolName,
        string? argumentsJson,
        string? resultJson,
        string status,
        DateTime startedAtUtc,
        DateTime? completedAtUtc = null,
        string? errorMessage = null);

    /// <summary>List all conversations for a user (most recent first).</summary>
    Task<List<AiConversation>> ListConversationsAsync(string userId);
}
