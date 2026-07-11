using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class FeedbackService : IFeedbackService
{
    private readonly ApplicationDbContext _db;
    private readonly IAbTestingService _abTestingService;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(ApplicationDbContext db, IAbTestingService abTestingService, ILogger<FeedbackService> logger)
    {
        _db = db;
        _abTestingService = abTestingService;
        _logger = logger;
    }

    public async Task SubmitExplicitFeedbackAsync(
        string userId, int conversationId, int messageId,
        int? rating, string? feedbackType, string? comment)
    {
        var feedback = new AiFeedback
        {
            ConversationId = conversationId,
            MessageId = messageId,
            UserId = userId,
            Rating = rating,
            FeedbackType = feedbackType,
            Comment = comment,
            FeedbackSource = "explicit",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AiFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        if (rating.HasValue)
        {
            var isSuccess = rating.Value >= 4;
            try
            {
                await _abTestingService.RecordOutcomeAsync(conversationId, isSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording A/B test outcome for conversation {ConversationId} on explicit feedback", conversationId);
            }
        }
    }

    public async Task DetectAndRecordImplicitFeedbackAsync(
        string userId, int conversationId, string userMessage, string agentName)
    {
        var signal = await DetectImplicitSignalAsync(userId, conversationId, userMessage);
        if (signal == null) return;

        var feedback = new AiFeedback
        {
            ConversationId = conversationId,
            UserId = userId,
            ImplicitSignal = signal,
            AgentName = agentName,
            FeedbackSource = "implicit",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AiFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Implicit signal '{Signal}' detected for user {UserId} in conversation {ConversationId}",
            signal, userId, conversationId);

        if (signal == "requested_human" || signal == "repeated_question" || signal == "ignored_recommendation")
        {
            try
            {
                await _abTestingService.RecordOutcomeAsync(conversationId, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording negative A/B test outcome for conversation {ConversationId} on implicit signal", conversationId);
            }
        }
    }

    private async Task<string?> DetectImplicitSignalAsync(
        string userId, int conversationId, string userMessage)
    {
        var msgLower = userMessage.ToLowerInvariant();

        // 1. Human support request detection (multi-language)
        var humanKeywords = new[]
        {
            "talk to human", "real person", "speak to agent", "customer support",
            "support team", "actual person", "müşteri hizmetleri", "yetkili",
            "insanla konuşmak", "تحدث مع موظف", "دعم العملاء", "بشر",
            "operator", "manager", "supervisor"
        };
        if (humanKeywords.Any(k => msgLower.Contains(k)))
            return "requested_human";

        // 2. Repeated question detection: compare with last 3 user messages
        var recentUserMessages = await _db.AiMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId &&
                        m.UserId == userId &&
                        m.Role == "user")
            .OrderByDescending(m => m.SequenceNumber)
            .Take(3)
            .Select(m => m.Content)
            .ToListAsync();

        if (recentUserMessages.Count >= 1)
        {
            var similarityThreshold = 0.6;
            foreach (var prevMsg in recentUserMessages)
            {
                var similarity = ComputeTextSimilarity(msgLower, prevMsg.ToLowerInvariant());
                if (similarity >= similarityThreshold)
                    return "repeated_question";
            }
        }

        // 3. Ignored recommendation detection: check if recommendations were given but cart is untouched
        var recentToolCalls = await _db.AiToolCalls
            .AsNoTracking()
            .Where(tc => tc.ConversationId == conversationId &&
                         tc.ToolName == "get_recommendations" &&
                         tc.Status == "Succeeded")
            .OrderByDescending(tc => tc.StartedAtUtc)
            .Take(1)
            .ToListAsync();

        if (recentToolCalls.Count > 0)
        {
            // Check if any add_to_cart happened AFTER the recommendation
            var hasAddedAfter = await _db.AiToolCalls
                .AnyAsync(tc => tc.ConversationId == conversationId &&
                                tc.ToolName == "add_to_cart" &&
                                tc.Status == "Succeeded" &&
                                tc.StartedAtUtc > recentToolCalls[0].StartedAtUtc);

            if (!hasAddedAfter && msgLower.Contains("no") || msgLower.Contains("not interested") ||
                msgLower.Contains("don't want") || msgLower.Contains("hayır") || msgLower.Contains("لا"))
            {
                return "ignored_recommendation";
            }
        }

        return null;
    }

    private static double ComputeTextSimilarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;

        var wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (wordsA.Length == 0 || wordsB.Length == 0) return 0;

        var setA = new HashSet<string>(wordsA);
        var setB = new HashSet<string>(wordsB);

        var intersection = setA.Intersect(setB, StringComparer.OrdinalIgnoreCase).Count();
        var union = setA.Union(setB, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0;
    }

    public async Task<object> GetConversationFeedbackAsync(int conversationId)
    {
        var feedbacks = await _db.AiFeedbacks
            .AsNoTracking()
            .Where(f => f.ConversationId == conversationId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync();

        return new
        {
            ConversationId = conversationId,
            Count = feedbacks.Count,
            ExplicitFeedback = feedbacks.Where(f => f.FeedbackSource == "explicit").Select(f => new
            {
                f.Id, f.Rating, f.FeedbackType, f.Comment, f.CreatedAtUtc
            }),
            ImplicitSignals = feedbacks.Where(f => f.FeedbackSource == "implicit").Select(f => new
            {
                f.Id, f.ImplicitSignal, f.AgentName, f.CreatedAtUtc
            })
        };
    }

    public async Task<object> GetFeedbackSummaryAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _db.AiFeedbacks.AsNoTracking();

        if (startDate.HasValue)
            query = query.Where(f => f.CreatedAtUtc >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(f => f.CreatedAtUtc <= endDate.Value);

        var feedbacks = await query.ToListAsync();

        var total = feedbacks.Count;
        var explicitFeedbacks = feedbacks.Where(f => f.FeedbackSource == "explicit" && f.Rating.HasValue).ToList();
        var avgRating = explicitFeedbacks.Count > 0 ? explicitFeedbacks.Average(f => f.Rating!.Value) : 0;

        var typeBreakdown = explicitFeedbacks
            .Where(f => !string.IsNullOrEmpty(f.FeedbackType))
            .GroupBy(f => f.FeedbackType!)
            .ToDictionary(g => g.Key, g => g.Count());

        var agentScores = explicitFeedbacks
            .Where(f => !string.IsNullOrEmpty(f.AgentName))
            .GroupBy(f => f.AgentName!)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(f => f.Rating!.Value), 2));

        var complaints = explicitFeedbacks
            .Where(f => !string.IsNullOrEmpty(f.Comment) && (f.Rating ?? 5) <= 2)
            .Select(f => f.Comment!)
            .Take(5)
            .ToList();

        return new
        {
            TotalFeedback = total,
            AverageRating = Math.Round(avgRating, 2),
            FeedbackTypeBreakdown = typeBreakdown,
            AgentSatisfactionScores = agentScores,
            TopComplaints = complaints,
            Period = new
            {
                Start = startDate?.ToString("yyyy-MM-dd"),
                End = endDate?.ToString("yyyy-MM-dd")
            }
        };
    }
}
