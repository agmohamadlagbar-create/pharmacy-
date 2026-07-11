using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IFeedbackService feedbackService, ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    [HttpPost("message")]
    [Authorize]
    public async Task<ActionResult> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _feedbackService.SubmitExplicitFeedbackAsync(
            userId, request.ConversationId, request.MessageId,
            request.Rating, request.FeedbackType, request.Comment);

        _logger.LogInformation(
            "Feedback submitted by user {UserId} for conversation {ConversationId}: rating={Rating}, type={FeedbackType}",
            userId, request.ConversationId, request.Rating, request.FeedbackType);

        return Ok(new { Success = true });
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Shop,Admin")]
    public async Task<ActionResult> GetFeedbackSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var result = await _feedbackService.GetFeedbackSummaryAsync(startDate, endDate);
        return Ok(result);
    }

    [HttpGet("conversation/{id}")]
    [Authorize]
    public async Task<ActionResult> GetConversationFeedback(int id)
    {
        var result = await _feedbackService.GetConversationFeedbackAsync(id);
        return Ok(result);
    }
}
