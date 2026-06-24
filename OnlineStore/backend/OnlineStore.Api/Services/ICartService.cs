namespace OnlineStore.Api.Services;

public interface ICartService
{
    Task<string> AddToCartAsync(string userId, int productId, int quantity);
    Task<string> UpdateQuantityAsync(string userId, int productId, int quantity);
    Task<string> RemoveFromCartAsync(string userId, int productId);
    Task<string> GetCartSummaryAsync(string userId);
}
