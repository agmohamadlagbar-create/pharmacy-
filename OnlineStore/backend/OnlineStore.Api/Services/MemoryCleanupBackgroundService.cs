namespace OnlineStore.Api.Services;

/// <summary>
/// Background service running hourly to clean up:
/// 1. Stale in-memory ConversationStateContext objects from ConversationStateManager.
/// 2. Expired database AI memories via IAiMemoryService.
/// </summary>
public class MemoryCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MemoryCleanupBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public MemoryCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MemoryCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MemoryCleanupBackgroundService started. Running cleanup tasks every hour.");

        // Wait 2 minutes after startup before running first cleanup
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting AI Memory and Conversation State cleanup cycle...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    // 1. Clean up stale in-memory references (24 hour inactivity cutoff)
                    var stateManager = scope.ServiceProvider.GetRequiredService<IConversationStateManager>();
                    stateManager.PruneStaleContexts(TimeSpan.FromHours(24));

                    // 2. Clean up expired database AI memories
                    var memoryService = scope.ServiceProvider.GetRequiredService<IAiMemoryService>();
                    await memoryService.EvictExpiredMemoriesAsync(stoppingToken);
                }

                _logger.LogInformation("AI Memory and Conversation State cleanup cycle completed successfully.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during AI Memory and Conversation State cleanup cycle.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
