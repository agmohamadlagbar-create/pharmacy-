using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

public interface IAiProviderClient
{
    string ProviderName { get; }

    Task<ChatCompletionResult> SendChatCompletionAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools = null,
        double? temperature = null,
        CancellationToken cancellationToken = default);

    Task<ChatCompletionResult> SendChatCompletionStreamingAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        double? temperature = null,
        CancellationToken cancellationToken = default);
}
