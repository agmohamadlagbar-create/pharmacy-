using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;

namespace OnlineStore.Api.Services;

public interface IBusinessAdvisorService
{
    // ═══════════════════════════════════════════════════
    //  Business Goals
    // ═══════════════════════════════════════════════════
    Task<BusinessGoal> CreateGoalAsync(string storeUserId, string title, string? description, string? targetMetric, decimal? targetValue);
    Task<List<BusinessGoal>> GetGoalsAsync(string storeUserId);
    Task<BusinessGoal?> GetGoalAsync(int goalId);
    Task UpdateGoalAsync(int goalId, string? title, string? description, decimal? currentProgress, GoalStatus? status);

    // ═══════════════════════════════════════════════════
    //  Recommendations
    // ═══════════════════════════════════════════════════
    Task<List<BusinessRecommendation>> GetActiveRecommendationsAsync(string storeUserId);
    Task<BusinessRecommendation> CreateRecommendationAsync(string storeUserId, int? goalId, string title, string description, string reason, RecommendationPriority priority, string? verificationCriteria, DateTime? dueDate);
    Task UpdateRecommendationStatusAsync(int id, RecommendationStatus status, string? verificationResult);
    Task<List<BusinessRecommendation>> GetRecommendationHistoryAsync(string storeUserId, int count = 20);

    // ═══════════════════════════════════════════════════
    //  Reports
    // ═══════════════════════════════════════════════════
    Task<BusinessAdvisorReport> SaveReportAsync(string storeUserId, string reportType, string content, int? healthScore, string? dataJson);
    Task<List<BusinessAdvisorReport>> GetRecentReportsAsync(string storeUserId, int count = 10);
    Task<BusinessAdvisorReport?> GetLatestReportAsync(string storeUserId);

    // ═══════════════════════════════════════════════════
    //  Memory
    // ═══════════════════════════════════════════════════
    Task SaveMemoryAsync(string storeUserId, string memoryType, string content, int importance);
    Task<List<BusinessAdvisorMemory>> GetMemoriesAsync(string storeUserId, int count = 30);

    // ═══════════════════════════════════════════════════
    //  Store Inspection
    // ═══════════════════════════════════════════════════
    Task<StoreInspectionResult> InspectStoreAsync(string storeUserId);
    Task<HealthScoreResponse> CalculateHealthScoreAsync(string storeUserId);

    // ═══════════════════════════════════════════════════
    //  Stores with active advisor
    // ═══════════════════════════════════════════════════
    Task<List<string>> GetStoresWithActiveAdvisorAsync();
}
