namespace OnlineStore.Api.Services;

public interface IMarketingService
{
    Task<object> GetMarketingInsightsAsync(string storeUserId);
    Task<object> SuggestPromotionsAsync(string storeUserId);
    Task<object> SuggestBundlesAsync(string storeUserId);
}
