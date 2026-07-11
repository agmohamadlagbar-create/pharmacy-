using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/admin-ai")]
[Authorize(Roles = "Admin")]
public class AdminAiController : ControllerBase
{
    private readonly IAiOrchestratorService _orchestratorService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AdminAiController> _logger;

    public AdminAiController(
        IAiOrchestratorService orchestratorService,
        IChatHistoryService chatHistoryService,
        ApplicationDbContext db,
        ILogger<AdminAiController> logger)
    {
        _orchestratorService = orchestratorService;
        _chatHistoryService = chatHistoryService;
        _db = db;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        int conversationId = request.ConversationId ?? 0;
        if (conversationId <= 0)
        {
            var conv = await _chatHistoryService.CreateConversationAsync(userId);
            conversationId = conv.Id;
        }

        var response = await _orchestratorService.ProcessMessageAsync(
            request.Message, userId, conversationId, "Admin");

        return Ok(new AiChatResponseDto
        {
            ConversationId = conversationId,
            Response = response
        });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var all = await _chatHistoryService.ListConversationsAsync(userId);
        var conversations = all.Skip((page - 1) * limit).Take(limit).ToList();
        return Ok(conversations);
    }

    [HttpGet("fraud-alerts")]
    public async Task<IActionResult> GetFraudAlerts([FromQuery] string? status)
    {
        var query = _db.FraudAlerts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        var alerts = await query.OrderByDescending(f => f.CreatedAtUtc).Select(f =>
            new FraudAlertDto(f.Id, f.AlertType, f.UserId, null, f.OrderIds,
                f.Description, f.Severity, f.Status, f.CreatedAtUtc, f.ResolvedAtUtc))
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpPatch("fraud-alerts/{id:int}")]
    public async Task<IActionResult> UpdateFraudAlert(int id, [FromBody] FraudAlertUpdateRequest request)
    {
        var alert = await _db.FraudAlerts.FindAsync(id);
        if (alert == null) return NotFound();

        alert.Status = request.Status;
        alert.ResolutionNotes = request.ResolutionNotes;
        if (request.Status is "resolved" or "dismissed")
        {
            alert.ResolvedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            alert.ResolvedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new MessageResponse($"Fraud alert {request.Status}"));
    }

    [HttpGet("moderation-flags")]
    public async Task<IActionResult> GetModerationFlags([FromQuery] string? status)
    {
        var query = _db.ModerationFlags.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status);

        var flags = await query.OrderByDescending(f => f.CreatedAtUtc).Select(f =>
            new ModerationFlagDto(f.Id, f.ContentType, f.ContentId, f.FlagType,
                f.Description, f.Severity, f.Status, f.CreatedAtUtc, f.ResolvedAtUtc))
            .ToListAsync();

        return Ok(flags);
    }

    [HttpPatch("moderation-flags/{id:int}")]
    public async Task<IActionResult> UpdateModerationFlag(int id, [FromBody] ModerationFlagUpdateRequest request)
    {
        var flag = await _db.ModerationFlags.FindAsync(id);
        if (flag == null) return NotFound();

        flag.Status = request.Status;
        if (request.Status is "resolved" or "dismissed")
        {
            flag.ResolvedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            flag.ResolvedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new MessageResponse($"Moderation flag {request.Status}"));
    }

    [HttpGet("security-events")]
    public async Task<IActionResult> GetSecurityEvents([FromQuery] string? severity, [FromQuery] int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var query = _db.SecurityEvents.AsNoTracking().Where(e => e.CreatedAtUtc >= since);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => e.Severity == severity);

        var events = await query.OrderByDescending(e => e.CreatedAtUtc).Select(e =>
            new SecurityEventDto(e.Id, e.EventType, e.UserId, e.IpAddress, e.RequestPath, e.Severity, e.CreatedAtUtc))
            .ToListAsync();

        return Ok(events);
    }
}
