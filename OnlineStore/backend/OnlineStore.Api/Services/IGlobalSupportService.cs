namespace OnlineStore.Api.Services;

public interface IGlobalSupportService
{
    Task<object> GetEscalatedIssuesAsync();
    Task<object> GetCrossStoreDisputesAsync();
    Task<object> GetUserHistoryAsync(string userId);
}
