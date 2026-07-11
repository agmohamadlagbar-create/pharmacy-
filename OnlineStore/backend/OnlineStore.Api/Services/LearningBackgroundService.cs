using Microsoft.EntityFrameworkCore;

namespace OnlineStore.Api.Services;

/// <summary>
/// Background service that runs daily AI learning analysis at 2:00 AM UTC.
/// The Learning Agent is NOT a user-facing agent — it's a scheduled job
/// that measures performance, detects failures, and generates improvement suggestions.
/// </summary>
public class LearningBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LearningBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public LearningBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LearningBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LearningBackgroundService started. Will run daily at 2:00 AM UTC.");

        // Wait 60 seconds after startup before first check
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Run at 2:00 AM UTC (check if current time is past 2:00 AM but before 3:00 AM)
                if (now.Hour == 2 && now.Minute < 5)
                {
                    var lastAnalysis = await GetLastAnalysisDateAsync(stoppingToken);

                    // Only run once per day
                    if (lastAnalysis?.Date != now.Date.AddDays(-1))
                    {
                        _logger.LogInformation("Starting scheduled daily AI learning analysis...");
                        using var scope = _serviceProvider.CreateScope();
                        var learningService = scope.ServiceProvider.GetRequiredService<ILearningService>();
                        await learningService.RunDailyAnalysisAsync(stoppingToken);
                        _logger.LogInformation("Scheduled daily analysis completed.");
                    }

                    // Wait an hour before checking again to avoid re-triggering
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in LearningBackgroundService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task<DateTime?> GetLastAnalysisDateAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        return await db.AiConversationAnalyses
            .AsNoTracking()
            .OrderByDescending(a => a.AnalysisDate)
            .Select(a => (DateTime?)a.AnalysisDate)
            .FirstOrDefaultAsync(ct);
    }
}
