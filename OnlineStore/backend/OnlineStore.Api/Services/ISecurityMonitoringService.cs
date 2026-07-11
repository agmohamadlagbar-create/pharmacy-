namespace OnlineStore.Api.Services;

public interface ISecurityMonitoringService
{
    Task<object> GetSecurityEventsAsync(int days = 7, string? severity = null);
    Task<object> GetLoginAnomaliesAsync(int days = 7);
    Task<object> GetRateLimitViolationsAsync(int days = 7);
}
