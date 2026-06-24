using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface IStoreSelectionService
{
    /// <summary>
    /// Finds the best store for a given product based on proximity, price, and stock.
    /// </summary>
    Task<StoreSelectionResultDto> FindBestStoreAsync(string productName, int quantity, double customerLat, double customerLon);
}
