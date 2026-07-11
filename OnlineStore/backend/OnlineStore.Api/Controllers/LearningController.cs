using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/learning")]
[Authorize(Roles = "Shop,Admin")]
public class LearningController : ControllerBase
{
    private readonly ILearningService _learningService;
    private readonly ILogger<LearningController> _logger;

    public LearningController(ILearningService learningService, ILogger<LearningController> logger)
    {
        _learningService = learningService;
        _logger = logger;
    }

    [HttpGet("analysis/latest")]
    public async Task<ActionResult> GetLatestAnalysis()
    {
        var result = await _learningService.GetLatestAnalysisAsync();
        return Ok(result);
    }

    [HttpGet("analysis/history")]
    public async Task<ActionResult> GetAnalysisHistory([FromQuery] int days = 30)
    {
        var result = await _learningService.GetAnalysisHistoryAsync(days);
        return Ok(result);
    }

    [HttpGet("knowledge-gaps")]
    public async Task<ActionResult> GetKnowledgeGaps([FromQuery] string? status = null)
    {
        var result = await _learningService.GetKnowledgeGapsAsync(status);
        return Ok(result);
    }

    [HttpPatch("knowledge-gaps/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateKnowledgeGap(int id, [FromBody] UpdateKnowledgeGapRequest request)
    {
        try
        {
            var result = await _learningService.UpdateKnowledgeGapAsync(id, request.Status, request.Resolution);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult> GetSuggestions([FromQuery] string? status = null)
    {
        var result = await _learningService.GetImprovementSuggestionsAsync(status);
        return Ok(result);
    }

    [HttpPost("suggestions/{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReviewSuggestion(int id, [FromBody] ReviewSuggestionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            await _learningService.ReviewSuggestionAsync(id, userId, request.Approved, request.Notes);
            return Ok(new { Success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("weekly-report")]
    public async Task<ActionResult> GetWeeklyReport([FromQuery] DateTime? weekStart = null)
    {
        var result = await _learningService.GetWeeklyReportAsync(weekStart);
        return Ok(result);
    }

    [HttpPost("run-analysis")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RunAnalysis()
    {
        _logger.LogInformation("Manual analysis triggered by admin user {UserId}",
            User.FindFirstValue(ClaimTypes.NameIdentifier));

        await _learningService.RunDailyAnalysisAsync();
        return Ok(new { Success = true, Message = "Analysis completed." });
    }
}
