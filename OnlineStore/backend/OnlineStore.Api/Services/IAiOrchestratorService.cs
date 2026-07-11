namespace OnlineStore.Api.Services;

public interface IAiOrchestratorService
{
    /// <summary>Process a message (backward-compatible, defaults to Customer role).</summary>
    Task<string> ProcessMessageAsync(string userMessage, string userId, int conversationId);

    /// <summary>Process a message with streaming (backward-compatible, defaults to Customer role).</summary>
    Task<string> ProcessMessageStreamingAsync(
        string userMessage, 
        string userId, 
        int conversationId, 
        Func<string, CancellationToken, Task> onToken, 
        CancellationToken cancellationToken = default);

    /// <summary>Process a message with role-aware multi-agent routing.</summary>
    Task<string> ProcessMessageAsync(string userMessage, string userId, int conversationId, string userRole);

    /// <summary>Process a message with streaming and role-aware multi-agent routing.</summary>
    Task<string> ProcessMessageStreamingAsync(
        string userMessage, 
        string userId, 
        int conversationId, 
        string userRole,
        Func<string, CancellationToken, Task> onToken, 
        CancellationToken cancellationToken = default);
}
