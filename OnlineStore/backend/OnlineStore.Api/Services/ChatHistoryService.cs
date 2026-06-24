using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly ApplicationDbContext _db;

    public ChatHistoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AiConversation> CreateConversationAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User context is required.");
        }

        var conversation = new AiConversation
        {
            UserId = userId,
            Title = "New Conversation",
            LastMessageAtUtc = null
        };

        _db.AiConversations.Add(conversation);
        await _db.SaveChangesAsync();
        return conversation;
    }

    public async Task<AiConversation?> GetConversationAsync(string userId, int conversationId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _db.AiConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
    }

    public async Task<List<GroqMessageDto>> LoadRecentMessagesAsync(string userId, int conversationId, int count = 20)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<GroqMessageDto>();
        }

        var exists = await _db.AiConversations
            .AsNoTracking()
            .AnyAsync(c => c.Id == conversationId && c.UserId == userId);

        if (!exists)
        {
            return new List<GroqMessageDto>();
        }

        var messages = await _db.AiMessages
            .Where(m => m.ConversationId == conversationId && m.UserId == userId)
            .OrderByDescending(m => m.SequenceNumber)
            .Take(count)
            .OrderBy(m => m.SequenceNumber)
            .Select(m => new GroqMessageDto
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToListAsync();

        return messages;
    }

    public async Task SaveMessageAsync(string userId, int conversationId, string role, string content)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User context is required.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Use a serializable transaction to atomically read max sequence + insert
            using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var conversation = await _db.AiConversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

                if (conversation is null)
                {
                    throw new UnauthorizedAccessException("Conversation does not belong to the authenticated user.");
                }

                var nextSequenceNumber = (await _db.AiMessages
                    .Where(m => m.ConversationId == conversationId)
                    .MaxAsync(m => (int?)m.SequenceNumber) ?? 0) + 1;

                var message = new AiMessage
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    SequenceNumber = nextSequenceNumber,
                    Role = role,
                    Content = content
                };

                _db.AiMessages.Add(message);

                var now = DateTime.UtcNow;
                conversation.UpdatedAtUtc = now;
                conversation.LastMessageAtUtc = now;

                // Auto-set title from first user message
                if (conversation.Title == "New Conversation" && role == "user")
                {
                    conversation.Title = content.Length > 60 ? content[..60] + "..." : content;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                // Detach all tracked entities so the next attempt starts clean
                foreach (var entry in _db.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }
                throw; // Re-throw so the execution strategy can retry
            }
        });
    }

    public async Task SaveToolCallAsync(
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
        string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User context is required.");
        }

        if (string.IsNullOrWhiteSpace(toolCallId))
        {
            throw new ArgumentException("Tool call id is required.", nameof(toolCallId));
        }

        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("Tool name is required.", nameof(toolName));
        }

        var conversation = await _db.AiConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation is null)
        {
            throw new UnauthorizedAccessException("Conversation does not belong to the authenticated user.");
        }

        if (messageId.HasValue)
        {
            var ownsMessage = await _db.AiMessages
                .AnyAsync(m => m.Id == messageId.Value && m.ConversationId == conversationId && m.UserId == userId);
            if (!ownsMessage)
            {
                throw new UnauthorizedAccessException("Message does not belong to the authenticated user conversation.");
            }
        }

        var toolCall = new AiToolCall
        {
            ConversationId = conversationId,
            MessageId = messageId,
            ToolCallId = toolCallId,
            ToolName = toolName,
            ArgumentsJson = argumentsJson,
            ResultJson = resultJson,
            Status = status,
            ErrorMessage = errorMessage,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc
        };

        _db.AiToolCalls.Add(toolCall);

        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<AiConversation>> ListConversationsAsync(string userId)
    {
        return await _db.AiConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync();
    }
}
