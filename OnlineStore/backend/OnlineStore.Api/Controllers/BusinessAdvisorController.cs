using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;
using OnlineStore.Api.Services.Agents;
using OnlineStore.Api.Services.Agents.ShopOwner;
using OnlineStore.Domain.Entities;

namespace OnlineStore.Api.Controllers;

/// <summary>
/// API controller for the Business Advisor feature.
/// Exposes endpoints for business goals, recommendations, health score,
/// reports, and on-demand report generation.
/// </summary>
[ApiController]
[Route("api/business-advisor")]
[Authorize(Roles = "Shop,Admin")]
public class BusinessAdvisorController : ControllerBase
{
    private readonly IBusinessAdvisorService _advisorService;
    private readonly BusinessAdvisorAgent _advisorAgent;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<BusinessAdvisorController> _logger;

    public BusinessAdvisorController(
        IBusinessAdvisorService advisorService,
        BusinessAdvisorAgent advisorAgent,
        IChatHistoryService chatHistoryService,
        ILogger<BusinessAdvisorController> logger)
    {
        _advisorService = advisorService;
        _advisorAgent = advisorAgent;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ═══════════════════════════════════════════════════
    //  Business Goals
    // ═══════════════════════════════════════════════════

    /// <summary>Create a new business goal.</summary>
    [HttpPost("goals")]
    public async Task<ActionResult<GoalResponse>> CreateGoal([FromBody] CreateGoalRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var goal = await _advisorService.CreateGoalAsync(
            userId, request.Title, request.Description,
            request.TargetMetric, request.TargetValue);

        return Ok(MapGoal(goal));
    }

    /// <summary>List all business goals.</summary>
    [HttpGet("goals")]
    public async Task<ActionResult<List<GoalResponse>>> GetGoals()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var goals = await _advisorService.GetGoalsAsync(userId);
        return Ok(goals.Select(MapGoal).ToList());
    }

    /// <summary>Update a business goal.</summary>
    [HttpPut("goals/{id}")]
    public async Task<ActionResult> UpdateGoal(int id, [FromBody] UpdateGoalRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var goal = await _advisorService.GetGoalAsync(id);
        if (goal == null || goal.StoreUserId != userId) return NotFound();

        await _advisorService.UpdateGoalAsync(id, request.Title, request.Description,
            request.CurrentProgress, request.Status);
        return Ok(new { Message = "Goal updated." });
    }

    // ═══════════════════════════════════════════════════
    //  Recommendations
    // ═══════════════════════════════════════════════════

    /// <summary>List active recommendations.</summary>
    [HttpGet("recommendations")]
    public async Task<ActionResult<List<RecommendationResponse>>> GetRecommendations()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var recs = await _advisorService.GetActiveRecommendationsAsync(userId);
        return Ok(recs.Select(MapRecommendation).ToList());
    }

    /// <summary>List recommendation history.</summary>
    [HttpGet("recommendations/history")]
    public async Task<ActionResult<List<RecommendationResponse>>> GetRecommendationHistory([FromQuery] int count = 20)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var recs = await _advisorService.GetRecommendationHistoryAsync(userId, count);
        return Ok(recs.Select(MapRecommendation).ToList());
    }

    /// <summary>Update a recommendation's status.</summary>
    [HttpPut("recommendations/{id}/status")]
    public async Task<ActionResult> UpdateRecommendationStatus(int id, [FromBody] UpdateRecommendationStatusRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _advisorService.UpdateRecommendationStatusAsync(id, request.Status, request.Result);
        return Ok(new { Message = "Recommendation status updated." });
    }

    // ═══════════════════════════════════════════════════
    //  Health Score
    // ═══════════════════════════════════════════════════

    /// <summary>Get the current business health score (0-100).</summary>
    [HttpGet("health-score")]
    public async Task<ActionResult<HealthScoreResponse>> GetHealthScore()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _advisorService.CalculateHealthScoreAsync(userId);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════
    //  Reports
    // ═══════════════════════════════════════════════════

    /// <summary>List recent advisor reports.</summary>
    [HttpGet("reports")]
    public async Task<ActionResult<List<ReportResponse>>> GetReports([FromQuery] int count = 10)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var reports = await _advisorService.GetRecentReportsAsync(userId, count);
        return Ok(reports.Select(MapReport).ToList());
    }

    /// <summary>Get the latest advisor report.</summary>
    [HttpGet("reports/latest")]
    public async Task<ActionResult<ReportResponse>> GetLatestReport()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var report = await _advisorService.GetLatestReportAsync(userId);
        if (report == null) return NotFound(new { Message = "No reports generated yet." });

        return Ok(MapReport(report));
    }

    /// <summary>Manually trigger a report generation.</summary>
    [HttpPost("reports/generate")]
    public async Task<ActionResult<ReportResponse>> GenerateReport([FromBody] GenerateReportRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var reportType = request.ReportType ?? "Daily";
        if (reportType is not ("Daily" or "Weekly" or "Monthly"))
            return BadRequest(new { Message = "ReportType must be Daily, Weekly, or Monthly." });

        var prompt = reportType switch
        {
            "Weekly" => "Generate a weekly business summary. Call get_store_inspection, get_business_goals, get_recommendation_history, get_business_health_score, and get_advisor_memories. Include: Weekly Revenue Summary, Best Selling Products, Growth Analysis, Inventory Risks, Suggested Priorities.",
            "Monthly" => "Generate a monthly strategy report. Call get_store_inspection, get_business_goals, get_recommendation_history, get_business_health_score, and get_advisor_memories. Include: Revenue Growth, Order Growth, Recommendation Success Rate, Completed Goals, Next Month Strategy.",
            _ => "Generate a daily business report. Call get_store_inspection, get_business_goals, get_recommendation_history, get_business_health_score, and get_advisor_memories. Verify pending recommendations. Create the next recommendation if appropriate."
        };

        // Create conversation and call agent
        var conversation = await _chatHistoryService.CreateConversationAsync(userId);
        var context = new AgentContext
        {
            UserId = userId,
            UserMessage = prompt,
            ConversationId = conversation.Id,
            UserRole = "Shop",
            ClassifiedIntent = "business_advisor",
            ConversationHistory = new List<GroqMessageDto> { new() { Role = "user", Content = prompt } }
        };

        var result = await _advisorAgent.ProcessAsync(context);

        if (!result.Success || string.IsNullOrWhiteSpace(result.ResponseText))
            return StatusCode(500, new { Message = "Failed to generate report." });

        var healthScore = await _advisorService.CalculateHealthScoreAsync(userId);
        var inspection = await _advisorService.InspectStoreAsync(userId);

        var report = await _advisorService.SaveReportAsync(
            userId, reportType, result.ResponseText, healthScore.Score,
            JsonSerializer.Serialize(inspection));

        return Ok(MapReport(report));
    }

    // ═══════════════════════════════════════════════════
    //  Store Inspection
    // ═══════════════════════════════════════════════════

    /// <summary>Get the current store inspection data.</summary>
    [HttpGet("store-inspection")]
    public async Task<ActionResult<StoreInspectionResult>> GetStoreInspection()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _advisorService.InspectStoreAsync(userId);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════
    //  Mapping Helpers
    // ═══════════════════════════════════════════════════

    private static GoalResponse MapGoal(BusinessGoal g) => new()
    {
        Id = g.Id,
        Title = g.Title,
        Description = g.Description,
        TargetMetric = g.TargetMetric,
        TargetValue = g.TargetValue,
        CurrentProgress = g.CurrentProgress,
        Status = g.Status.ToString(),
        CreatedAtUtc = g.CreatedAtUtc,
        UpdatedAtUtc = g.UpdatedAtUtc
    };

    private static RecommendationResponse MapRecommendation(BusinessRecommendation r) => new()
    {
        Id = r.Id,
        GoalId = r.GoalId,
        Title = r.Title,
        Description = r.Description,
        Reason = r.Reason,
        Priority = r.Priority.ToString(),
        Status = r.Status.ToString(),
        VerificationCriteria = r.VerificationCriteria,
        VerificationResult = r.VerificationResult,
        CreatedAtUtc = r.CreatedAtUtc,
        DueDate = r.DueDate,
        CompletedAtUtc = r.CompletedAtUtc
    };

    private static ReportResponse MapReport(BusinessAdvisorReport r) => new()
    {
        Id = r.Id,
        ReportType = r.ReportType,
        ReportContent = r.ReportContent,
        HealthScore = r.HealthScore,
        CreatedAtUtc = r.CreatedAtUtc
    };
}
