using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Controllers;

/// <summary>
/// API controller for shop owner AI interactions.
/// Provides REST endpoint for non-streaming shop AI requests,
/// and conversation management for shop owners.
/// </summary>
[ApiController]
[Route("api/shop-ai")]
[Authorize(Roles = "Shop,Admin")]
public class ShopAiController : ControllerBase
{
    private readonly IAiOrchestratorService _orchestratorService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ShopAiController> _logger;

    public ShopAiController(
        IAiOrchestratorService orchestratorService,
        IChatHistoryService chatHistoryService,
        ILogger<ShopAiController> logger)
    {
        _orchestratorService = orchestratorService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Non-streaming shop AI chat endpoint.
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponseDto>> Chat([FromBody] AiChatRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        int conversationId = request.ConversationId ?? 0;

        if (conversationId <= 0)
        {
            var conv = await _chatHistoryService.CreateConversationAsync(userId);
            conversationId = conv.Id;
        }

        var response = await _orchestratorService.ProcessMessageAsync(
            request.Message, userId, conversationId, "Shop");

        return Ok(new AiChatResponseDto
        {
            Response = response,
            ConversationId = conversationId
        });
    }

    /// <summary>
    /// List all shop AI conversations for the authenticated shop owner.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult> GetConversations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var conversations = await _chatHistoryService.ListConversationsAsync(userId);
        var shopConversations = conversations
            .Where(c => c.AgentType == "shop_owner" || c.AgentType == null)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.LastMessageAtUtc,
                c.AgentType,
                c.LastAgentUsed,
                c.CreatedAtUtc
            })
            .ToList();

        return Ok(shopConversations);
    }
}
