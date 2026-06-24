using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface IProductSearchService
{
    Task<ProductSearchSummaryDto> SearchProductsAsync(string productName, double userLat, double userLon);
}
