using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SecurityMonitoringService> _logger;

    public SecurityMonitoringService(ApplicationDbContext db, ILogger<SecurityMonitoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetSecurityEventsAsync(int days = 7, string? severity = null)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var query = _db.SecurityEvents.Where(e => e.CreatedAtUtc >= since);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => e.Severity == severity);

        var events = await query.OrderByDescending(e => e.CreatedAtUtc).ToListAsync();

        return new
        {
            TotalEvents = events.Count,
            ByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
            BySeverity = events.GroupBy(e => e.Severity).ToDictionary(g => g.Key, g => g.Count()),
            Events = events.Take(50).Select(e => new
            {
                e.Id, e.EventType, e.UserId, e.IpAddress, e.RequestPath, e.Severity, e.CreatedAtUtc
            })
        };
    }

    public async Task<object> GetLoginAnomaliesAsync(int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var users = await _db.Users
            .Where(u => u.AccessFailedCount > 5 || (u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow))
            .Select(u => new
            {
                u.Id, u.Email, u.FullName, u.AccessFailedCount,
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                u.LockoutEnd
            })
            .ToListAsync();

        return new
        {
            TotalAnomalies = users.Count,
            LockedAccounts = users.Count(u => u.IsLockedOut),
            HighFailedLoginUsers = users.Where(u => !u.IsLockedOut).ToList(),
            Users = users
        };
    }

    public async Task<object> GetRateLimitViolationsAsync(int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var violations = await _db.SecurityEvents
            .Where(e => e.EventType == "rate_limited" && e.CreatedAtUtc >= since)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync();

        return new
        {
            TotalViolations = violations.Count,
            TopIps = violations
                .GroupBy(v => v.IpAddress ?? "unknown")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() }),
            Recent = violations.Take(50).Select(v => new
            {
                v.Id, v.IpAddress, v.RequestPath, v.UserId, v.CreatedAtUtc
            })
        };
    }
}
