using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[EnableRateLimiting("AiRateLimit")]
public class AiController : ControllerBase
{
    private readonly IAiOrchestratorService _aiOrchestratorService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiOrchestratorService aiOrchestratorService, IChatHistoryService chatHistoryService, ILogger<AiController> logger)
    {
        _aiOrchestratorService = aiOrchestratorService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(AiChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.GetAuthenticatedUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "User context not found." });

        try
        {
            var conversation = request.ConversationId.HasValue
                ? await _chatHistoryService.GetConversationAsync(userId, request.ConversationId.Value)
                : null;

            if (request.ConversationId.HasValue && conversation is null)
            {
                return NotFound(new { error = "Conversation not found." });
            }

            conversation ??= await _chatHistoryService.CreateConversationAsync(userId);

            var aiResponse = await _aiOrchestratorService.ProcessMessageAsync(request.Message, userId, conversation.Id);

            return Ok(new AiChatResponseDto
            {
                Response = aiResponse,
                ConversationId = conversation.Id
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized conversation access attempt.");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in AI chat request. ExceptionType: {ExceptionType}, Message: {ErrorMessage}, InnerException: {InnerMessage}",
                ex.GetType().FullName,
                ex.Message,
                ex.InnerException?.Message ?? "(none)");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = $"An error occurred while processing your request: {ex.Message}" });
        }
    }

    [HttpGet("conversations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConversations()
    {
        var userId = User.GetAuthenticatedUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "User context not found." });

        var conversations = await _chatHistoryService.ListConversationsAsync(userId);

        var result = conversations.Select(c => new
        {
            c.Id,
            c.Title,
            CreatedAt = c.CreatedAtUtc,
            UpdatedAt = c.UpdatedAtUtc
        });

        return Ok(result);
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessages(int conversationId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "User context not found." });

        var conversation = await _chatHistoryService.GetConversationAsync(userId, conversationId);
        if (conversation is null)
            return NotFound(new { error = "Conversation not found." });

        var messages = await _chatHistoryService.LoadRecentMessagesAsync(userId, conversationId, 20);

        return Ok(messages.Select(m => new { m.Role, m.Content }));
    }
}
