using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Hubs;

[Authorize(Roles = "Admin")]
public class AdminAiChatHub : Hub
{
    private readonly IAiOrchestratorService _orchestratorService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<AdminAiChatHub> _logger;

    public AdminAiChatHub(
        IAiOrchestratorService orchestratorService,
        IChatHistoryService chatHistoryService,
        ILogger<AdminAiChatHub> logger)
    {
        _orchestratorService = orchestratorService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    public async Task<ChannelReader<AiChatStreamChunkDto>> SendMessage(AiChatRequestDto request)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new HubException("Authentication required.");

        var channel = Channel.CreateUnbounded<AiChatStreamChunkDto>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = WriteToChannel(channel.Writer, userId, request);

        return channel.Reader;
    }

    private async Task WriteToChannel(ChannelWriter<AiChatStreamChunkDto> writer, string userId, AiChatRequestDto request)
    {
        int conversationId = request.ConversationId ?? 0;

        try
        {
            if (conversationId <= 0)
            {
                var conv = await _chatHistoryService.CreateConversationAsync(userId);
                conversationId = conv.Id;
            }
            else
            {
                var existingConv = await _chatHistoryService.GetConversationAsync(userId, conversationId);
                if (existingConv == null)
                {
                    var conv = await _chatHistoryService.CreateConversationAsync(userId);
                    conversationId = conv.Id;
                }
            }

            await writer.WriteAsync(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.ConversationStarted,
                ConversationId = conversationId
            });

            var fullResponse = await _orchestratorService.ProcessMessageStreamingAsync(
                request.Message,
                userId,
                conversationId,
                "Admin",
                async (token, ct) =>
                {
                    if (token.StartsWith("[SYSTEM_EVENT:"))
                    {
                        var eventContent = token.TrimStart('[').TrimEnd(']');
                        var parts = eventContent.Split(':', 3);
                        var eventType = parts.Length > 1 ? parts[1] : "unknown";

                        if (eventType == "cart_updated")
                        {
                            await writer.WriteAsync(new AiChatStreamChunkDto
                            {
                                EventType = AiChatStreamEventTypes.CartUpdated,
                                ConversationId = conversationId
                            }, ct);
                        }
                        return;
                    }

                    await writer.WriteAsync(new AiChatStreamChunkDto
                    {
                        EventType = AiChatStreamEventTypes.Token,
                        ConversationId = conversationId,
                        Token = token
                    }, ct);
                },
                Context.ConnectionAborted);

            var messageId = await _chatHistoryService.GetLastAssistantMessageIdAsync(userId, conversationId);

            await writer.WriteAsync(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.Completed,
                ConversationId = conversationId,
                FullResponse = fullResponse,
                MessageId = messageId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AdminAiChatHub stream for user {UserId}", userId);
            await writer.WriteAsync(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.Error,
                ConversationId = conversationId,
                Error = "An error occurred while processing your request. Please try again."
            });
        }
        finally
        {
            writer.Complete();
        }
    }
}
