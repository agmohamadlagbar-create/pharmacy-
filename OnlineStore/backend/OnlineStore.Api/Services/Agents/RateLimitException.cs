namespace OnlineStore.Api.Services.Agents;

public class RateLimitException : HttpRequestException
{
    public string ProviderName { get; }
    public TimeSpan? RetryAfter { get; }

    public RateLimitException(string providerName, TimeSpan? retryAfter = null)
        : base($"Rate limit reached on {providerName}.")
    {
        ProviderName = providerName;
        RetryAfter = retryAfter;
    }
}
