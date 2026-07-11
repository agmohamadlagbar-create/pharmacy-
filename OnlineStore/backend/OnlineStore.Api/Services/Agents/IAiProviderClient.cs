using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

public interface IAiProviderClient
{
    string ProviderName { get; }

    Task<ChatCompletionResult> SendChatCompletionAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools = null,
        CancellationToken cancellationToken = default);

    Task<ChatCompletionResult> SendChatCompletionStreamingAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default);
}
