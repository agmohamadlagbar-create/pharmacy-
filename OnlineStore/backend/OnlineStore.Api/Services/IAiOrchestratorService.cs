namespace OnlineStore.Api.Services;

public interface IAiOrchestratorService
{
    Task<string> ProcessMessageAsync(string userMessage, string userId, int conversationId);
    
    Task<string> ProcessMessageStreamingAsync(
        string userMessage, 
        string userId, 
        int conversationId, 
        Func<string, CancellationToken, Task> onToken, 
        CancellationToken cancellationToken = default);
}
