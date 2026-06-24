using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services;

public interface IRecommendationService
{
    /// <summary>
    /// Get product recommendations for a user, optionally scoped to a specific product context.
    /// </summary>
    Task<RecommendationResultDto> GetRecommendationsAsync(string userId, int? productId = null);
}
