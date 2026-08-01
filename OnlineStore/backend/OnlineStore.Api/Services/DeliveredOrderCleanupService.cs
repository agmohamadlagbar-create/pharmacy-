using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Background service that automatically deletes delivered and cancelled orders older than 7 days.
/// Runs once every 6 hours to check for and remove expired delivered and cancelled orders,
/// their associated order items, and related delivery jobs.
/// </summary>
public class DeliveredOrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeliveredOrderCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public DeliveredOrderCleanupService(IServiceProvider serviceProvider, ILogger<DeliveredOrderCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeliveredOrderCleanupService started. Running every 6 hours.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up delivered and cancelled orders.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task DoCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-7);

        _logger.LogInformation("Cleaning up delivered and cancelled orders older than {Cutoff}...", cutoff);

        // Find all delivered or cancelled orders older than 7 days
        var expiredOrderIds = await db.Orders
            .Where(o => (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Cancelled) && o.UpdatedAtUtc < cutoff)
            .Select(o => o.Id)
            .ToListAsync();

        if (expiredOrderIds.Count == 0)
        {
            _logger.LogInformation("No expired delivered or cancelled orders found.");
            return;
        }

        _logger.LogInformation("Found {Count} delivered or cancelled orders older than 7 days. Deleting...", expiredOrderIds.Count);

        // Delete related delivery jobs first (foreign key constraint)
        var deletedJobs = await db.DeliveryJobs
            .Where(j => expiredOrderIds.Contains(j.OrderId))
            .ExecuteDeleteAsync();

        // Delete order items (foreign key constraint)
        var deletedItems = await db.OrderItems
            .Where(i => expiredOrderIds.Contains(i.OrderId))
            .ExecuteDeleteAsync();

        // Delete the orders themselves
        var deletedOrders = await db.Orders
            .Where(o => expiredOrderIds.Contains(o.Id))
            .ExecuteDeleteAsync();

        _logger.LogInformation(
            "Cleanup complete: deleted {Orders} orders, {Items} order items, {Jobs} delivery jobs.",
            deletedOrders, deletedItems, deletedJobs);
    }
}
