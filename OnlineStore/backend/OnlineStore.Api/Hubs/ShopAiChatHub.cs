using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Hubs;

/// <summary>
/// SignalR hub for shop owner AI streaming.
/// Same streaming pattern as AiChatHub but with shop-owner role enforcement.
/// </summary>
[Authorize(Roles = "Shop,Admin")]
public class ShopAiChatHub : Hub
{
    private readonly IAiOrchestratorService _orchestratorService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ShopAiChatHub> _logger;

    public ShopAiChatHub(
        IAiOrchestratorService orchestratorService,
        IChatHistoryService chatHistoryService,
        ILogger<ShopAiChatHub> logger)
    {
        _orchestratorService = orchestratorService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Stream an AI response for a shop owner.
    /// The client invokes this method, and receives tokens via the returned ChannelReader.
    /// </summary>
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

    public async Task<ChannelReader<AiChatStreamChunkDto>> StreamChat(AiChatRequestDto request)
    {
        return await SendMessage(request);
    }

    private async Task WriteToChannel(ChannelWriter<AiChatStreamChunkDto> writer, string userId, AiChatRequestDto request)
    {
        int conversationId = request.ConversationId ?? 0;

        try
        {
            // Create or validate conversation
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

            // Send conversation ID to client
            await writer.WriteAsync(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.ConversationStarted,
                ConversationId = conversationId
            });

            // Stream the response via multi-agent pipeline (Shop role)
            var fullResponse = await _orchestratorService.ProcessMessageStreamingAsync(
                request.Message,
                userId,
                conversationId,
                "Shop",
                async (token, ct) =>
                {
                    // Handle system events
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

                    // Regular token
                    await writer.WriteAsync(new AiChatStreamChunkDto
                    {
                        EventType = AiChatStreamEventTypes.Token,
                        ConversationId = conversationId,
                        Token = token
                    }, ct);
                },
                Context.ConnectionAborted);

            // Send completion event
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
            _logger.LogError(ex, "Error in ShopAiChatHub stream for user {UserId}", userId);
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
