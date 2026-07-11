namespace OnlineStore.Api.Services;

public interface IModerationService
{
    Task<object> GetFlaggedContentAsync(string? contentType = null);
    Task<object> GetReviewModerationAsync();
    Task<object> GetPricingAnomaliesAsync();
}
