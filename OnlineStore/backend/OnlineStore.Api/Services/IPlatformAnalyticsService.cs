namespace OnlineStore.Api.Services;

public interface IPlatformAnalyticsService
{
    Task<object> GetPlatformOverviewAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<object> GetStoreRankingsAsync(string metric = "revenue", int top = 10);
    Task<object> GetCategoryTrendsAsync(int days = 30);
    Task<object> GetRegionalAnalyticsAsync();
}
