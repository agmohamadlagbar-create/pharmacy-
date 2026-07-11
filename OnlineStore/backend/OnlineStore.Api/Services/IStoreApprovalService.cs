namespace OnlineStore.Api.Services;

public interface IStoreApprovalService
{
    Task<object> GetPendingStoresAsync();
    Task<object> GetStoreRiskScoreAsync(string storeUserId);
    Task<object> GetStoreComplianceCheckAsync(string storeUserId);
}
