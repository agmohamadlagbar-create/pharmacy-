namespace OnlineStore.Api.Services;

/// <summary>
/// Inventory intelligence service providing stock analysis,
/// restock suggestions, and sales velocity tracking for shop owners.
/// </summary>
public interface IInventoryIntelligenceService
{
    Task<object> GetLowStockProductsAsync(string storeUserId);
    Task<object> GetFastSellingProductsAsync(string storeUserId, int days = 7);
    Task<object> GetRestockSuggestionsAsync(string storeUserId);
    Task<object> GetInventoryHealthScoreAsync(string storeUserId);
    Task<object> GetSeasonalDemandForecastAsync(string storeUserId);
}
