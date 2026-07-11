using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services.Agents;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class LearningService : ILearningService
{
    private readonly ApplicationDbContext _db;
    private readonly AiGateway _gateway;
    private readonly ILogger<LearningService> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public LearningService(ApplicationDbContext db, AiGateway gateway, ILogger<LearningService> logger)
    {
        _db = db;
        _gateway = gateway;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    //  Daily Analysis
    // ═══════════════════════════════════════════════════

    public async Task RunDailyAnalysisAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting daily AI learning analysis...");

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        // 1. Conversation analysis
        var analysis = await AnalyzeConversationsAsync(yesterday, today, ct);

        // 2. Knowledge gap detection
        await DetectKnowledgeGapsAsync(yesterday, today, ct);

        // 3. Tool usage analysis
        await AnalyzeToolUsageAsync(yesterday, today, ct);

        // 4. Generate improvement suggestions
        await GenerateImprovementSuggestionsAsync(yesterday, today, ct);

        // Save analysis
        _db.AiConversationAnalyses.Add(analysis);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Daily learning analysis completed for {Date}", yesterday.ToString("yyyy-MM-dd"));
    }

    private async Task<AiConversationAnalysis> AnalyzeConversationsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var conversations = await _db.AiConversations
            .AsNoTracking()
            .Where(c => c.CreatedAtUtc >= from && c.CreatedAtUtc < to)
            .ToListAsync(ct);

        var total = conversations.Count;
        var abandoned = conversations.Count(c => c.LastMessageAtUtc == null ||
            (DateTime.UtcNow - c.LastMessageAtUtc.Value).TotalHours > 24);

        // Feedback stats for the period
        var feedbacks = await _db.AiFeedbacks
            .AsNoTracking()
            .Where(f => f.CreatedAtUtc >= from && f.CreatedAtUtc < to)
            .ToListAsync(ct);

        var explicitRatings = feedbacks.Where(f => f.Rating.HasValue).ToList();
        var avgRating = explicitRatings.Count > 0 ? explicitRatings.Average(f => f.Rating!.Value) : 0;
        var negativeCount = explicitRatings.Count(f => f.Rating <= 2);

        // Per-agent performance
        var agentPerformance = feedbacks
            .Where(f => !string.IsNullOrEmpty(f.AgentName) && f.Rating.HasValue)
            .GroupBy(f => f.AgentName!)
            .ToDictionary(g => g.Key, g => new
            {
                AvgRating = Math.Round(g.Average(f => f.Rating!.Value), 2),
                Count = g.Count()
            });

        // Common questions (extract from user messages)
        var userMessages = await _db.AiMessages
            .AsNoTracking()
            .Where(m => m.Role == "user" &&
                        m.CreatedAtUtc >= from && m.CreatedAtUtc < to)
            .Select(m => m.Content)
            .Take(200)
            .ToListAsync(ct);

        var shortMessages = userMessages
            .Where(m => m.Length < 200 && m.Length > 5)
            .GroupBy(m => m.ToLowerInvariant().Trim())
            .OrderByDescending(g => g.Count())
            .Take(20)
            .Select(g => new { Question = g.Key, Count = g.Count() })
            .ToList();

        // Tool performance
        var toolCalls = await _db.AiToolCalls
            .AsNoTracking()
            .Where(tc => tc.StartedAtUtc >= from && tc.StartedAtUtc < to)
            .ToListAsync(ct);

        var toolPerf = toolCalls
            .GroupBy(tc => tc.ToolName)
            .ToDictionary(g => g.Key, g => new
            {
                TotalCalls = g.Count(),
                SuccessRate = g.Count(tc => tc.Status == "Succeeded") * 100.0 / g.Count(),
                AvgDurationMs = g.Where(tc => tc.CompletedAtUtc.HasValue)
                    .Select(tc => (tc.CompletedAtUtc!.Value - tc.StartedAtUtc).TotalMilliseconds)
                    .DefaultIfEmpty(0)
                    .Average()
            });

        return new AiConversationAnalysis
        {
            AnalysisDate = from.Date,
            TotalConversations = total,
            SuccessfulConversations = total - negativeCount,
            FailedConversations = negativeCount,
            AbandonedConversations = abandoned,
            AverageSatisfactionRating = Math.Round(avgRating, 2),
            TotalFeedbackCount = feedbacks.Count,
            NegativeFeedbackCount = negativeCount,
            AgentPerformanceJson = JsonSerializer.Serialize(agentPerformance),
            TopIssuesJson = JsonSerializer.Serialize(feedbacks
                .Where(f => !string.IsNullOrEmpty(f.FeedbackType))
                .GroupBy(f => f.FeedbackType!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new { Issue = g.Key, Count = g.Count() })),
            CommonQuestionsJson = JsonSerializer.Serialize(shortMessages),
            ToolPerformanceJson = JsonSerializer.Serialize(toolPerf)
        };
    }

    private class LowRatedResponseInfo
    {
        public string? UserQuestion { get; set; }
        public string AssistantResponse { get; set; } = "";
        public string? FeedbackType { get; set; }
        public string? Comment { get; set; }
    }

    private async Task DetectKnowledgeGapsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        // Find tool calls that failed or returned empty results
        var failedToolCalls = await _db.AiToolCalls
            .AsNoTracking()
            .Where(tc => tc.StartedAtUtc >= from && tc.StartedAtUtc < to &&
                         (tc.Status == "Failed" || (tc.ResultJson != null && tc.ResultJson == "[]")))
            .Select(tc => new { tc.ToolName, tc.ErrorMessage, tc.ResultJson })
            .Take(50)
            .ToListAsync(ct);

        // Find "I don't know" patterns in assistant messages
        var unsureMessages = await _db.AiMessages
            .AsNoTracking()
            .Where(m => m.Role == "assistant" &&
                        m.CreatedAtUtc >= from && m.CreatedAtUtc < to &&
                        (m.Content.ToLower().Contains("i don't know") ||
                         m.Content.ToLower().Contains("i'm not sure") ||
                         m.Content.ToLower().Contains("i cannot") ||
                         m.Content.ToLower().Contains("i am not able") ||
                         m.Content.ToLower().Contains("bilmiyorum") ||
                         m.Content.ToLower().Contains("emin değilim")))
            .Select(m => m.Content)
            .Take(50)
            .ToListAsync(ct);

        // Find assistant messages that received a low rating (explicit feedback Rating <= 3)
        var lowRatedFeedback = await _db.AiFeedbacks
            .AsNoTracking()
            .Where(f => f.CreatedAtUtc >= from && f.CreatedAtUtc < to && f.Rating != null && f.Rating <= 3 && f.MessageId != null)
            .Select(f => new { f.MessageId, f.Comment, f.FeedbackType })
            .Take(20)
            .ToListAsync(ct);

        var lowRatedMessageIds = lowRatedFeedback.Select(f => f.MessageId!.Value).ToList();

        var lowRatedMessages = await _db.AiMessages
            .AsNoTracking()
            .Where(m => lowRatedMessageIds.Contains(m.Id))
            .Select(m => new { m.Id, m.ConversationId, m.Content, m.SequenceNumber })
            .ToListAsync(ct);

        var lowRatedList = new List<LowRatedResponseInfo>();
        foreach (var fb in lowRatedFeedback)
        {
            var msg = lowRatedMessages.FirstOrDefault(m => m.Id == fb.MessageId);
            if (msg == null) continue;

            // Find the preceding message in the same conversation
            var precedingUserMessage = await _db.AiMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == msg.ConversationId && m.Role == "user" && m.SequenceNumber < msg.SequenceNumber)
                .OrderByDescending(m => m.SequenceNumber)
                .Select(m => m.Content)
                .FirstOrDefaultAsync(ct);

            lowRatedList.Add(new LowRatedResponseInfo
            {
                UserQuestion = precedingUserMessage,
                AssistantResponse = msg.Content,
                FeedbackType = fb.FeedbackType,
                Comment = fb.Comment
            });
        }

        // Use LLM to identify patterns
        if (failedToolCalls.Count > 0 || unsureMessages.Count > 0 || lowRatedList.Count > 0)
        {
            try
            {
                var failedCallText = string.Join("\n", failedToolCalls.Take(10).Select(tc =>
                    $"- Tool: {tc.ToolName}, Error: {tc.ErrorMessage ?? "Empty result"}"));
                var llmPrompt = BuildKnowledgeGapPrompt(failedCallText, unsureMessages, lowRatedList);
                var messages = new List<GroqMessageDto>
                {
                    new() { Role = "system", Content = llmPrompt },
                    new() { Role = "user", Content = "Analyze the above data and identify knowledge gaps." }
                };

                var aiContext = new AiRequestContext { AgentName = "learning_service", Complexity = TaskComplexity.Background };
                var result = await _gateway.SendAsync(
                    aiContext, messages, null, ct);

                var gaps = ParseKnowledgeGaps(result.Content);
                foreach (var gap in gaps)
                {
                    var existing = await _db.AiKnowledgeGaps
                        .FirstOrDefaultAsync(g => g.Topic == gap.Topic && g.Status == "detected", ct);

                    if (existing != null)
                    {
                        existing.OccurrenceCount += gap.OccurrenceCount;
                        existing.LastDetectedAtUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        _db.AiKnowledgeGaps.Add(new AiKnowledgeGap
                        {
                            Topic = gap.Topic,
                            Description = gap.Description,
                            ExampleQuestions = gap.ExampleQuestions,
                            OccurrenceCount = gap.OccurrenceCount,
                            Status = "detected"
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM knowledge gap detection failed");
            }
        }
    }

    private async Task AnalyzeToolUsageAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var toolCalls = await _db.AiToolCalls
            .AsNoTracking()
            .Where(tc => tc.StartedAtUtc >= from && tc.StartedAtUtc < to)
            .ToListAsync(ct);

        if (toolCalls.Count == 0) return;

        var slowTools = toolCalls
            .Where(tc => tc.CompletedAtUtc.HasValue)
            .GroupBy(tc => tc.ToolName)
            .Select(g => new
            {
                Tool = g.Key,
                AvgDurationMs = g.Average(tc => (tc.CompletedAtUtc!.Value - tc.StartedAtUtc).TotalMilliseconds),
                SuccessRate = g.Count(tc => tc.Status == "Succeeded") * 100.0 / g.Count()
            })
            .Where(t => t.AvgDurationMs > 5000 || t.SuccessRate < 80)
            .ToList();

        if (slowTools.Count > 0)
        {
            _logger.LogWarning("Slow/failing tools detected: {Tools}",
                JsonSerializer.Serialize(slowTools));

            foreach (var tool in slowTools)
            {
                if (tool.SuccessRate < 80)
                {
                    var existing = await _db.AiImprovementSuggestions
                        .FirstOrDefaultAsync(s =>
                            s.Category == "tool" &&
                            s.Title.Contains(tool.Tool) &&
                            s.Status == "pending", ct);

                    if (existing == null)
                    {
                        _db.AiImprovementSuggestions.Add(new AiImprovementSuggestion
                        {
                            Category = "tool",
                            AgentName = "system",
                            Title = $"Tool '{tool.Tool}' has low success rate ({tool.SuccessRate:N0}%)",
                            Description = $"The tool '{tool.Tool}' has a success rate of {tool.SuccessRate:N0}%. " +
                                          "Consider reviewing the tool implementation or error handling.",
                            SuggestedChange = $"Review tool '{tool.Tool}' error handling and edge cases.",
                            Priority = tool.SuccessRate < 50 ? 1 : 3,
                            Status = "pending"
                        });
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task GenerateImprovementSuggestionsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var feedbacks = await _db.AiFeedbacks
            .AsNoTracking()
            .Where(f => f.CreatedAtUtc >= from && f.CreatedAtUtc < to &&
                        f.FeedbackSource == "explicit" && f.Rating <= 2)
            .ToListAsync(ct);

        if (feedbacks.Count < 3) return;

        var agentLowRatings = feedbacks
            .Where(f => !string.IsNullOrEmpty(f.AgentName))
            .GroupBy(f => f.AgentName!)
            .Select(g => new { Agent = g.Key, LowRatingCount = g.Count(), Total = g.Count() })
            .OrderByDescending(g => g.LowRatingCount)
            .Take(3)
            .ToList();

        foreach (var agent in agentLowRatings)
        {
            var existingSuggestion = await _db.AiImprovementSuggestions
                .FirstOrDefaultAsync(s =>
                    s.Category == "prompt" &&
                    s.AgentName == agent.Agent &&
                    s.Status == "pending", ct);

            if (existingSuggestion == null)
            {
                _db.AiImprovementSuggestions.Add(new AiImprovementSuggestion
                {
                    Category = "prompt",
                    AgentName = agent.Agent,
                    Title = $"Low satisfaction for {agent.Agent} agent",
                    Description = $"The '{agent.Agent}' agent received {agent.LowRatingCount} low ratings " +
                                  $"out of {agent.Total} total feedback entries in the last 24 hours. " +
                                  "Consider reviewing the agent's system prompt for clarity and accuracy.",
                    SuggestedChange = $"Review and optimize the {agent.Agent} agent's system prompt.",
                    Priority = agent.Total >= 5 ? 2 : 3,
                    Status = "pending"
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string BuildKnowledgeGapPrompt(string failedToolCallText, List<string> unsureMessages, List<LowRatedResponseInfo> lowRatedResponses)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are an AI system analyst. Analyze the following data from customer support interactions and identify knowledge gaps.");
        sb.AppendLine("A knowledge gap is a topic where the AI frequently cannot answer or provides poor responses.");
        sb.AppendLine();
        sb.AppendLine("Return your analysis as a JSON array. Each element must have:");
        sb.AppendLine("  - \"topic\": short snake_case identifier (e.g., \"international_shipping\")");
        sb.AppendLine("  - \"description\": human-readable description of the gap");
        sb.AppendLine("  - \"example_questions\": comma-separated sample user questions");
        sb.AppendLine("  - \"occurrence_count\": approximate number of times this gap appears");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the JSON array, no other text.");

        if (!string.IsNullOrWhiteSpace(failedToolCallText))
        {
            sb.AppendLine();
            sb.AppendLine("Failed/empty tool calls:");
            sb.AppendLine(failedToolCallText);
        }

        if (unsureMessages.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Assistant messages expressing uncertainty:");
            foreach (var msg in unsureMessages.Take(10))
            {
                var truncated = msg.Length > 200 ? msg[..200] + "..." : msg;
                sb.AppendLine($"- \"{truncated}\"");
            }
        }

        if (lowRatedResponses.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Low-rated assistant responses (rated poorly by users):");
            foreach (var r in lowRatedResponses.Take(15))
            {
                sb.AppendLine($"- User Question: \"{r.UserQuestion ?? "N/A"}\"");
                sb.AppendLine($"  Assistant Response: \"{(r.AssistantResponse.Length > 200 ? r.AssistantResponse[..200] + "..." : r.AssistantResponse)}\"");
                sb.AppendLine($"  User Feedback Tag: {r.FeedbackType ?? "None"}");
                if (!string.IsNullOrWhiteSpace(r.Comment))
                {
                    sb.AppendLine($"  User Comment: \"{r.Comment}\"");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private List<(string Topic, string Description, string? ExampleQuestions, int OccurrenceCount)> ParseKnowledgeGaps(string llmResponse)
    {
        try
        {
            var jsonStart = llmResponse.IndexOf('[');
            var jsonEnd = llmResponse.LastIndexOf(']');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = llmResponse[jsonStart..(jsonEnd + 1)];
                var items = JsonSerializer.Deserialize<List<JsonElement>>(json, JsonOpts);
                if (items != null)
                {
                    return items.Select(item => (
                        Topic: item.GetProperty("topic").GetString() ?? "unknown",
                        Description: item.GetProperty("description").GetString() ?? "",
                        ExampleQuestions: item.TryGetProperty("example_questions", out var eq) ? eq.GetString() : null,
                        OccurrenceCount: item.TryGetProperty("occurrence_count", out var oc) ? oc.GetInt32() : 1
                    )).ToList();
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse knowledge gaps from LLM response");
        }

        return new List<(string, string, string?, int)>();
    }

    // ═══════════════════════════════════════════════════
    //  Query Methods
    // ═══════════════════════════════════════════════════

    public async Task<object> GetLatestAnalysisAsync()
    {
        var analysis = await _db.AiConversationAnalyses
            .AsNoTracking()
            .OrderByDescending(a => a.AnalysisDate)
            .FirstOrDefaultAsync();

        if (analysis == null)
            return new { Message = "No analysis data yet. Run daily analysis first." };

        return new
        {
            analysis.Id,
            analysis.AnalysisDate,
            analysis.TotalConversations,
            analysis.SuccessfulConversations,
            analysis.FailedConversations,
            analysis.AbandonedConversations,
            analysis.AverageSatisfactionRating,
            analysis.TotalFeedbackCount,
            analysis.NegativeFeedbackCount,
            AgentPerformance = SafeDeserialize<object>(analysis.AgentPerformanceJson),
            TopIssues = SafeDeserialize<object>(analysis.TopIssuesJson),
            CommonQuestions = SafeDeserialize<object>(analysis.CommonQuestionsJson),
            ToolPerformance = SafeDeserialize<object>(analysis.ToolPerformanceJson),
            SuccessRate = analysis.TotalConversations > 0
                ? Math.Round(analysis.SuccessfulConversations * 100.0 / analysis.TotalConversations, 1)
                : 0
        };
    }

    public async Task<object> GetAnalysisHistoryAsync(int days = 30)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        var analyses = await _db.AiConversationAnalyses
            .AsNoTracking()
            .Where(a => a.AnalysisDate >= since)
            .OrderByDescending(a => a.AnalysisDate)
            .ToListAsync();

        return analyses.Select(a => new
        {
            a.Id,
            a.AnalysisDate,
            a.TotalConversations,
            a.AverageSatisfactionRating,
            a.NegativeFeedbackCount,
            a.AbandonedConversations
        }).ToList();
    }

    public async Task<List<object>> GetKnowledgeGapsAsync(string? status = null)
    {
        var query = _db.AiKnowledgeGaps.AsNoTracking();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(g => g.Status == status);

        var gaps = await query.OrderByDescending(g => g.OccurrenceCount).ToListAsync();
        return gaps.Select(g => (object)new
        {
            g.Id, g.Topic, g.Description, g.ExampleQuestions,
            g.OccurrenceCount, g.Status, g.FirstDetectedAtUtc, g.LastDetectedAtUtc, g.ResolvedAtUtc, g.Resolution
        }).ToList();
    }

    public async Task<List<object>> GetImprovementSuggestionsAsync(string? status = null)
    {
        var query = _db.AiImprovementSuggestions.AsNoTracking();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var suggestions = await query.OrderByDescending(s => s.Priority).ThenByDescending(s => s.CreatedAtUtc).ToListAsync();
        return suggestions.Select(s => (object)new
        {
            s.Id, s.Category, s.AgentName, s.Title, s.Description,
            s.SuggestedChange, s.Status, s.Priority, s.ReviewedBy, s.ReviewNotes,
            s.CreatedAtUtc, s.ReviewedAtUtc
        }).ToList();
    }

    public async Task ReviewSuggestionAsync(int suggestionId, string reviewerId, bool approved, string? notes = null)
    {
        var suggestion = await _db.AiImprovementSuggestions.FindAsync(suggestionId);
        if (suggestion == null) throw new KeyNotFoundException($"Suggestion {suggestionId} not found.");

        suggestion.Status = approved ? "approved" : "rejected";
        suggestion.ReviewedBy = reviewerId;
        suggestion.ReviewNotes = notes;
        suggestion.ReviewedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<object> UpdateKnowledgeGapAsync(int gapId, string status, string? resolution = null)
    {
        var gap = await _db.AiKnowledgeGaps.FindAsync(gapId);
        if (gap == null) throw new KeyNotFoundException($"Knowledge gap {gapId} not found.");

        gap.Status = status;
        if (!string.IsNullOrEmpty(resolution))
            gap.Resolution = resolution;
        if (status == "resolved")
            gap.ResolvedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new { Success = true, gap.Id, gap.Status };
    }

    public async Task<WeeklyReportDto> GetWeeklyReportAsync(DateTime? weekStart = null)
    {
        var endDate = weekStart?.AddDays(7) ?? DateTime.UtcNow;
        var startDate = weekStart ?? endDate.AddDays(-7);

        var analyses = await _db.AiConversationAnalyses
            .AsNoTracking()
            .Where(a => a.AnalysisDate >= startDate && a.AnalysisDate < endDate)
            .ToListAsync();

        var avgSatisfaction = analyses.Count > 0 ? analyses.Average(a => a.AverageSatisfactionRating) : 0;
        var totalConversations = analyses.Sum(a => a.TotalConversations);
        var totalSuccessful = analyses.Sum(a => a.SuccessfulConversations);
        var successRate = totalConversations > 0 ? Math.Round(totalSuccessful * 100.0 / totalConversations, 1) : 0;

        // Top questions
        var commonQuestions = analyses
            .Where(a => !string.IsNullOrEmpty(a.CommonQuestionsJson))
            .SelectMany(a => SafeDeserialize<List<JsonElement>>(a.CommonQuestionsJson) ?? new List<JsonElement>())
            .GroupBy(q => q.GetProperty("question").GetString() ?? "")
            .OrderByDescending(g => g.Sum(q => q.GetProperty("count").GetInt32()))
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        // Knowledge gaps
        var knowledgeGaps = await _db.AiKnowledgeGaps
            .AsNoTracking()
            .Where(g => g.Status == "detected")
            .OrderByDescending(g => g.OccurrenceCount)
            .Take(3)
            .Select(g => g.Topic)
            .ToListAsync();

        // Agent ranking
        var agentRankings = analyses
            .Where(a => !string.IsNullOrEmpty(a.AgentPerformanceJson))
            .SelectMany(a => SafeDeserialize<Dictionary<string, JsonElement>>(a.AgentPerformanceJson) ?? new())
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key,
                g => g.Average(kvp => kvp.Value.GetProperty("AvgRating").GetDouble()));

        var pendingCount = await _db.AiImprovementSuggestions
            .CountAsync(s => s.Status == "pending");

        return new WeeklyReportDto
        {
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            AverageSatisfaction = Math.Round(avgSatisfaction, 2),
            TotalConversations = totalConversations,
            SuccessRate = successRate,
            TopQuestions = commonQuestions,
            KnowledgeGaps = knowledgeGaps,
            AgentRanking = agentRankings.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2)),
            PendingSuggestions = pendingCount
        };
    }

    private static T? SafeDeserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch (JsonException) { return null; }
    }
}
