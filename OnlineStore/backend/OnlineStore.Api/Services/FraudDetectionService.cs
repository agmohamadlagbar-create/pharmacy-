using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(ApplicationDbContext db, ILogger<FraudDetectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> DetectSuspiciousOrdersAsync(int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var alerts = new List<object>();

        // Order velocity: 10+ orders in 1 hour
        var fastOrders = await _db.Orders
            .Where(o => o.CreatedAtUtc >= since)
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                RecentOrders = g.Count(),
                HourlyOrders = g.Count(o => o.CreatedAtUtc >= DateTime.UtcNow.AddHours(-1))
            })
            .Where(g => g.HourlyOrders >= 10)
            .ToListAsync();

        foreach (var entry in fastOrders)
        {
            alerts.Add(new { AlertType = "order_velocity", UserId = entry.UserId, Severity = "high", Description = $"{entry.HourlyOrders} orders in the last hour (threshold: 10)" });
        }

        // Amount anomaly: order total > 5x user's average
        var userAverages = await _db.Orders
            .Where(o => o.CreatedAtUtc >= since)
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                AvgAmount = g.Average(o => o.TotalAmount)
            })
            .ToListAsync();

        foreach (var avg in userAverages.Where(a => a.AvgAmount > 0))
        {
            var anomalies = await _db.Orders
                .Where(o => o.UserId == avg.UserId && o.CreatedAtUtc >= since && o.TotalAmount > avg.AvgAmount * 5)
                .Select(o => new { o.Id, o.TotalAmount })
                .ToListAsync();

            if (anomalies.Any())
            {
                alerts.Add(new
                {
                    AlertType = "amount_anomaly",
                    UserId = avg.UserId,
                    Severity = "medium",
                    Description = $"Order(s) exceed 5x the user's average of {avg.AvgAmount:C}",
                    OrderIds = string.Join(",", anomalies.Select(a => a.Id))
                });
            }
        }

        // Rapid cancellation: 5+ cancelled orders in a day
        var cancellations = await _db.Orders
            .Where(o => o.CreatedAtUtc >= since && o.Status == Domain.Entities.OrderStatus.Cancelled)
            .GroupBy(o => new { o.UserId, Date = o.CreatedAtUtc.Date })
            .Select(g => new { g.Key.UserId, g.Key.Date, Count = g.Count() })
            .Where(g => g.Count >= 5)
            .ToListAsync();

        foreach (var entry in cancellations)
        {
            alerts.Add(new { AlertType = "rapid_cancellation", UserId = entry.UserId, Severity = "medium", Description = $"{entry.Count} cancelled orders on {entry.Date:yyyy-MM-dd}" });
        }

        // Refund abuse: >30% cancellation rate
        var cancellationRates = await _db.Orders
            .Where(o => o.CreatedAtUtc >= since)
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Total = g.Count(),
                Cancelled = g.Count(o => o.Status == Domain.Entities.OrderStatus.Cancelled)
            })
            .Where(g => g.Total >= 5 && (double)g.Cancelled / g.Total > 0.3)
            .ToListAsync();

        foreach (var entry in cancellationRates)
        {
            alerts.Add(new { AlertType = "refund_abuse", UserId = entry.UserId, Severity = "medium", Description = $"Cancellation rate: {entry.Cancelled}/{entry.Total} ({(double)entry.Cancelled / entry.Total:P0})" });
        }

        // New account bulk orders: account <24h old placing >3 orders
        var cutoff = DateTime.UtcNow.AddDays(-1);
        var newUsers = await _db.Users
            .Where(u => u.CreatedAtUtc >= cutoff)
            .Select(u => u.Id)
            .ToListAsync();

        if (newUsers.Any())
        {
            var bulkOrders = await _db.Orders
                .Where(o => newUsers.Contains(o.UserId) && o.CreatedAtUtc >= cutoff)
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(g => g.Count > 3)
                .ToListAsync();

            foreach (var entry in bulkOrders)
            {
                alerts.Add(new { AlertType = "new_account_bulk", UserId = entry.UserId, Severity = "high", Description = $"Account <24h old with {entry.Count} orders" });
            }
        }

        // Persist alerts
        foreach (var alert in alerts)
        {
            var type = alert.GetType();
            _db.FraudAlerts.Add(new FraudAlert
            {
                AlertType = (string)type.GetProperty("AlertType")!.GetValue(alert)!,
                UserId = (string?)type.GetProperty("UserId")?.GetValue(alert),
                OrderIds = (string?)type.GetProperty("OrderIds")?.GetValue(alert),
                Description = (string)type.GetProperty("Description")!.GetValue(alert)!,
                Severity = (string)type.GetProperty("Severity")!.GetValue(alert)!,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new { TotalAlerts = alerts.Count, Alerts = alerts };
    }

    public async Task<object> DetectDuplicateAccountsAsync()
    {
        var users = await _db.Users.ToListAsync();
        var duplicates = new List<object>();

        var emailPrefixGroups = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Email) && u.Email!.Contains('@'))
            .GroupBy(u => u.Email!.Split('@')[0].ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in emailPrefixGroups)
        {
            duplicates.Add(new
            {
                Pattern = $"email_prefix:{group.Key}",
                UserIds = group.Select(u => u.Id).ToList(),
                Severity = "medium",
                Description = $"{group.Count()} accounts share the same email prefix '{group.Key}'"
            });
        }

        return new { TotalDuplicateGroups = duplicates.Count, Duplicates = duplicates };
    }

    public async Task<object> GetFraudSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _db.FraudAlerts.AsQueryable();
        if (startDate.HasValue) query = query.Where(f => f.CreatedAtUtc >= startDate.Value);
        if (endDate.HasValue) query = query.Where(f => f.CreatedAtUtc <= endDate.Value);

        var alerts = await query.OrderByDescending(f => f.CreatedAtUtc).ToListAsync();

        return new
        {
            Total = alerts.Count,
            ByType = alerts.GroupBy(a => a.AlertType).ToDictionary(g => g.Key, g => g.Count()),
            BySeverity = alerts.GroupBy(a => a.Severity).ToDictionary(g => g.Key, g => g.Count()),
            ByStatus = alerts.GroupBy(a => a.Status).ToDictionary(g => g.Key, g => g.Count()),
            Recent = alerts.Take(20).Select(a => new
            {
                a.Id, a.AlertType, a.UserId, a.Description, a.Severity, a.Status, a.CreatedAtUtc
            })
        };
    }
}
