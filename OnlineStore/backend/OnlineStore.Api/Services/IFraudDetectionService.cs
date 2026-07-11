namespace OnlineStore.Api.Services;

public interface IFraudDetectionService
{
    Task<object> DetectSuspiciousOrdersAsync(int days = 7);
    Task<object> DetectDuplicateAccountsAsync();
    Task<object> GetFraudSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
}
