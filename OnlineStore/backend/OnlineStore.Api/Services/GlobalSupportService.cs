using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class GlobalSupportService : IGlobalSupportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GlobalSupportService> _logger;

    public GlobalSupportService(ApplicationDbContext db, ILogger<GlobalSupportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetEscalatedIssuesAsync()
    {
        var escalatedFeedbacks = await _db.AiFeedbacks
            .Where(f => f.ImplicitSignal == "human_support_request")
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(50)
            .ToListAsync();

        var result = new List<object>();
        foreach (var fb in escalatedFeedbacks)
        {
            var user = await _db.Users.FindAsync(fb.UserId);
            result.Add(new
            {
                fb.ConversationId,
                UserId = fb.UserId,
                UserEmail = user?.Email,
                Summary = fb.Comment ?? "User requested human support",
                fb.CreatedAtUtc
            });
        }

        return new { TotalEscalated = result.Count, Issues = result };
    }

    public async Task<object> GetCrossStoreDisputesAsync()
    {
        var multiStoreOrders = await _db.Orders
            .Where(o => o.OrderGroupId != null)
            .GroupBy(o => o.OrderGroupId)
            .Where(g => g.Select(o => o.StoreUserId).Distinct().Count() > 1)
            .Select(g => new
            {
                OrderGroupId = g.Key,
                Orders = g.Select(o => new { o.Id, o.StoreUserId })
            })
            .Take(20)
            .ToListAsync();

        var result = new List<object>();
        foreach (var group in multiStoreOrders)
        {
            var firstOrder = await _db.Orders
                .Where(o => o.OrderGroupId == group.OrderGroupId)
                .OrderBy(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (firstOrder != null)
            {
                result.Add(new
                {
                    OrderId = firstOrder.Id,
                    UserId = firstOrder.UserId,
                    OrderGroupId = group.OrderGroupId,
                    StoreIds = group.Orders.Select(o => o.StoreUserId).Distinct().ToList(),
                    Description = $"Order group {group.OrderGroupId} spans {group.Orders.Select(o => o.StoreUserId).Distinct().Count()} stores",
                    firstOrder.CreatedAtUtc
                });
            }
        }

        return new { TotalDisputes = result.Count, Disputes = result };
    }

    public async Task<object> GetUserHistoryAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return new { Error = "User not found" };

        var orders = await _db.Orders.Where(o => o.UserId == userId).ToListAsync();
        var orderCount = orders.Count;
        var cancelledCount = orders.Count(o => o.Status == Domain.Entities.OrderStatus.Cancelled);
        var cancellationRate = orderCount > 0 ? (double)cancelledCount / orderCount : 0;

        var feedbackCount = await _db.AiFeedbacks.CountAsync(f => f.UserId == userId);
        var aiConvCount = await _db.AiConversations.CountAsync(c => c.UserId == userId);

        var isLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

        return new
        {
            UserId = userId,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAtUtc = user.CreatedAtUtc,
            OrderCount = orderCount,
            CancellationRate = Math.Round(cancellationRate * 100, 1),
            FeedbackCount = feedbackCount,
            AiConversationCount = aiConvCount,
            IsLockedOut = isLockedOut
        };
    }
}
