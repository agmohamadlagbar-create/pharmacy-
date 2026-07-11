using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface ILearningService
{
    Task RunDailyAnalysisAsync(CancellationToken ct = default);
    Task<object> GetLatestAnalysisAsync();
    Task<List<object>> GetKnowledgeGapsAsync(string? status = null);
    Task<List<object>> GetImprovementSuggestionsAsync(string? status = null);
    Task ReviewSuggestionAsync(int suggestionId, string reviewerId, bool approved, string? notes = null);
    Task<WeeklyReportDto> GetWeeklyReportAsync(DateTime? weekStart = null);
    Task<object> GetAnalysisHistoryAsync(int days = 30);
    Task<object> UpdateKnowledgeGapAsync(int gapId, string status, string? resolution = null);
}
