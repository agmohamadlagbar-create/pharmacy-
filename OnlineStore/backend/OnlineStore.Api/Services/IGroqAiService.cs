namespace OnlineStore.Api.Services;

public interface IGroqAiService
{
    Task<string> AskAsync(string userMessage, string userId, int conversationId);
    Task<string> AskStreamingAsync(
        string userMessage,
        string userId,
        int conversationId,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default);
}
