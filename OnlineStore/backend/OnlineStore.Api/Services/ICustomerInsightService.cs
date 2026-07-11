namespace OnlineStore.Api.Services;

public interface ICustomerInsightService
{
    Task<object> GetCustomerBehaviorAnalysisAsync(string storeUserId);
    Task<object> GetCustomerSatisfactionStatsAsync(string storeUserId);
    Task<object> GetTopCustomersAsync(string storeUserId);
}
