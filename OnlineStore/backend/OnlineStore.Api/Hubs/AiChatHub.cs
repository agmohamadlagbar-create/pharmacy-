using System.Threading.Channels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AiChatHub : Hub
{
    private const int StreamBufferSize = 256;
    private const int MaxMessageLength = 2000;

    private readonly IAiOrchestratorService _aiOrchestratorService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<AiChatHub> _logger;

    public AiChatHub(
        IAiOrchestratorService aiOrchestratorService,
        IChatHistoryService chatHistoryService,
        ILogger<AiChatHub> logger)
    {
        _aiOrchestratorService = aiOrchestratorService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    public ChannelReader<AiChatStreamChunkDto> StreamChat(AiChatRequestDto request, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<AiChatStreamChunkDto>(new BoundedChannelOptions(StreamBufferSize)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        _ = WriteStreamAsync(channel.Writer, request, cancellationToken);
        return channel.Reader;
    }

    private async Task WriteStreamAsync(
        ChannelWriter<AiChatStreamChunkDto> writer,
        AiChatRequestDto? request,
        CancellationToken clientStreamCancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            clientStreamCancellationToken,
            Context.ConnectionAborted);
        var cancellationToken = linkedCts.Token;

        var conversationId = 0;

        try
        {
            var userId = Context.User.GetAuthenticatedUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                await writer.WriteAsync(new AiChatStreamChunkDto
                {
                    EventType = AiChatStreamEventTypes.Error,
                    Error = "User context not found."
                }, cancellationToken);
                return;
            }

            if (request is null || string.IsNullOrWhiteSpace(request.Message))
            {
                await writer.WriteAsync(new AiChatStreamChunkDto
                {
                    EventType = AiChatStreamEventTypes.Error,
                    Error = "Message is required."
                }, cancellationToken);
                return;
            }

            if (request.Message.Length > MaxMessageLength)
            {
                await writer.WriteAsync(new AiChatStreamChunkDto
                {
                    EventType = AiChatStreamEventTypes.Error,
                    Error = $"Message cannot exceed {MaxMessageLength} characters."
                }, cancellationToken);
                return;
            }

            var conversation = request.ConversationId.HasValue
                ? await _chatHistoryService.GetConversationAsync(userId, request.ConversationId.Value)
                : null;

            if (request.ConversationId.HasValue && conversation is null)
            {
                await writer.WriteAsync(new AiChatStreamChunkDto
                {
                    EventType = AiChatStreamEventTypes.Error,
                    Error = "Conversation not found."
                }, cancellationToken);
                return;
            }

            conversation ??= await _chatHistoryService.CreateConversationAsync(userId);
            conversationId = conversation.Id;

            await writer.WriteAsync(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.ConversationStarted,
                ConversationId = conversationId
            }, cancellationToken);

            var fullResponse = await _aiOrchestratorService.ProcessMessageStreamingAsync(
                request.Message,
                userId,
                conversationId,
                async (token, ct) =>
                {
                    if (string.IsNullOrEmpty(token)) return;

                    if (token?.Trim() == "[SYSTEM_EVENT:cart_updated]")
                    {
                        _logger.LogInformation("Sending CartUpdated event to user {UserId} in conversation {ConversationId}", userId, conversationId);
                        await writer.WriteAsync(new AiChatStreamChunkDto
                        {
                            EventType = AiChatStreamEventTypes.CartUpdated,
                            ConversationId = conversationId
                        }, ct);
                        return;
                    }

                    if (token?.Trim().StartsWith("[SYSTEM_EVENT:recommendations:") == true)
                    {
                        // Extract JSON payload: remove prefix "[SYSTEM_EVENT:recommendations:" and trailing "]"
                        var raw = token.Trim();
                        var jsonPayload = raw.Substring(
                            "[SYSTEM_EVENT:recommendations:".Length,
                            raw.Length - "[SYSTEM_EVENT:recommendations:".Length - 1);

                        _logger.LogInformation("Sending Recommendations event to user {UserId} in conversation {ConversationId}", userId, conversationId);
                        await writer.WriteAsync(new AiChatStreamChunkDto
                        {
                            EventType = AiChatStreamEventTypes.Recommendations,
                            ConversationId = conversationId,
                            FullResponse = jsonPayload
                        }, ct);
                        return;
                    }

                    await writer.WriteAsync(new AiChatStreamChunkDto
                    {
                        EventType = AiChatStreamEventTypes.Token,
                        ConversationId = conversationId,
                        Token = token
                    }, ct);
                },
                cancellationToken);

            var messageId = await _chatHistoryService.GetLastAssistantMessageIdAsync(userId, conversationId);

            await writer.WriteAsync(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.Completed,
                ConversationId = conversationId,
                FullResponse = fullResponse,
                MessageId = messageId
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "AI streaming was cancelled for connection {ConnectionId} and conversation {ConversationId}.",
                Context.ConnectionId,
                conversationId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized AI stream attempt for connection {ConnectionId}.",
                Context.ConnectionId);

            writer.TryWrite(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.Error,
                ConversationId = conversationId,
                Error = "Unauthorized conversation access."
            });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message;
            _logger.LogError(
                ex,
                "AI streaming failed for connection {ConnectionId} and conversation {ConversationId}. " +
                "ExceptionType: {ExceptionType}, Message: {ErrorMessage}, InnerException: {InnerMessage}",
                Context.ConnectionId,
                conversationId,
                ex.GetType().FullName,
                ex.Message,
                innerMsg ?? "(none)");

            // Surface a more specific error message when possible
            var userMessage = ex switch
            {
                Microsoft.EntityFrameworkCore.DbUpdateException dbEx =>
                    $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}",
                HttpRequestException httpEx =>
                    $"AI service communication error: {httpEx.Message}",
                TimeoutException =>
                    "The request timed out. Please try again.",
                _ =>
                    $"An error occurred while processing your request: {ex.Message}"
            };

            writer.TryWrite(new AiChatStreamChunkDto
            {
                EventType = AiChatStreamEventTypes.Error,
                ConversationId = conversationId,
                Error = userMessage
            });
        }
        finally
        {
            writer.TryComplete();
        }
    }
}
