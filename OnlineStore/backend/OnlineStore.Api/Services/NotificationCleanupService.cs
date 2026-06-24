using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Background service that cleans up old notifications periodically.
/// </summary>
public class NotificationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1);

    public NotificationCleanupService(IServiceProvider serviceProvider, ILogger<NotificationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationCleanupService started. Running every 1 day.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up notifications.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task DoCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-1);
        
        _logger.LogInformation("Cleaning up notifications older than {Cutoff}...", cutoff);

        var deletedCount = await db.Notifications
            .Where(n => n.CreatedAtUtc < cutoff)
            .ExecuteDeleteAsync();

        if (deletedCount > 0)
        {
            _logger.LogInformation("Successfully deleted {Count} old notifications.", deletedCount);
        }
    }
}
