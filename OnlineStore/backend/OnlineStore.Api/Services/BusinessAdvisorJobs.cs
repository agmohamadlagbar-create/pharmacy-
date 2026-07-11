using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services.Agents;
using OnlineStore.Api.Services.Agents.ShopOwner;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Hangfire recurring jobs for the BusinessAdvisorAgent.
/// Generates daily, weekly, and monthly reports for stores with active business goals.
/// Each job: loads context → calls the AI agent → saves the report → sends a notification.
/// </summary>
public class BusinessAdvisorJobs
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusinessAdvisorJobs> _logger;

    public BusinessAdvisorJobs(
        IServiceProvider serviceProvider,
        ILogger<BusinessAdvisorJobs> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 09:00. Generates daily report + verifies pending recommendations.
    /// </summary>
    public async Task GenerateDailyReportsAsync()
    {
        await GenerateReportsAsync("Daily",
            "Generate a daily business report. " +
            "1) First call get_store_inspection to review the store's current state. " +
            "2) Call get_business_goals to check active goals. " +
            "3) Call get_recommendation_history to check pending recommendations. " +
            "4) Call get_advisor_memories to recall past context. " +
            "5) Verify if pending recommendations were completed by comparing current store data. " +
            "6) If completed, update the recommendation status and celebrate. If not, remind the merchant. " +
            "7) Call get_business_health_score and include it in the report. " +
            "8) Create a new recommendation only if all pending ones are handled. " +
            "9) Save any important observations using save_advisor_memory. " +
            "Write a coaching-style daily report.");
    }

    /// <summary>
    /// Runs every Sunday at 18:00. Generates weekly summary.
    /// </summary>
    public async Task GenerateWeeklyReportsAsync()
    {
        await GenerateReportsAsync("Weekly",
            "Generate a weekly business summary. " +
            "1) Call get_store_inspection for current store state. " +
            "2) Call get_business_goals to review goal progress. " +
            "3) Call get_recommendation_history to summarize this week's recommendation outcomes. " +
            "4) Call get_business_health_score for the weekly score. " +
            "5) Call get_advisor_memories for context. " +
            "Include: Weekly Revenue Summary, Best Selling Products, Worst Selling Products, " +
            "Growth Analysis, Inventory Risks, Customer Trends, Suggested Priorities for next week. " +
            "Save a weekly strategy observation using save_advisor_memory.");
    }

    /// <summary>
    /// Runs on the 1st of each month at 09:00. Generates monthly strategy report.
    /// </summary>
    public async Task GenerateMonthlyReportsAsync()
    {
        await GenerateReportsAsync("Monthly",
            "Generate a monthly strategy report. " +
            "1) Call get_store_inspection for comprehensive store state. " +
            "2) Call get_business_goals to evaluate monthly goal progress. " +
            "3) Call get_recommendation_history to calculate Recommendation Success Rate. " +
            "4) Call get_business_health_score for the monthly score. " +
            "5) Call get_advisor_memories for strategic context. " +
            "Include: Revenue Growth, Order Growth, Conversion Improvements, " +
            "Recommendation Success Rate, Completed Goals, Next Month Strategy. " +
            "Update goal progress if measurable. " +
            "Save a monthly milestone using save_advisor_memory.");
    }

    private async Task GenerateReportsAsync(string reportType, string prompt)
    {
        _logger.LogInformation("[BusinessAdvisorJobs] Starting {ReportType} report generation", reportType);

        List<string> storeUserIds;
        using (var scope = _serviceProvider.CreateScope())
        {
            var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
            storeUserIds = await advisorService.GetStoresWithActiveAdvisorAsync();
        }

        if (storeUserIds.Count == 0)
        {
            _logger.LogInformation("[BusinessAdvisorJobs] No stores with active advisor. Skipping.");
            return;
        }

        _logger.LogInformation("[BusinessAdvisorJobs] Processing {Count} stores for {ReportType} reports",
            storeUserIds.Count, reportType);

        foreach (var storeUserId in storeUserIds)
        {
            try
            {
                await GenerateReportForStoreAsync(storeUserId, reportType, prompt);

                // Throttle between stores to avoid API rate limits
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BusinessAdvisorJobs] Failed to generate {ReportType} report for store {StoreUserId}",
                    reportType, storeUserId);
            }
        }

        _logger.LogInformation("[BusinessAdvisorJobs] Completed {ReportType} report generation for {Count} stores",
            reportType, storeUserIds.Count);
    }

    private async Task GenerateReportForStoreAsync(string storeUserId, string reportType, string prompt)
    {
        using var scope = _serviceProvider.CreateScope();

        var advisorAgent = scope.ServiceProvider.GetRequiredService<BusinessAdvisorAgent>();
        var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
        var chatHistoryService = scope.ServiceProvider.GetRequiredService<IChatHistoryService>();

        // Create a temporary conversation for the background job
        var conversation = await chatHistoryService.CreateConversationAsync(storeUserId);

        var context = new AgentContext
        {
            UserId = storeUserId,
            UserMessage = prompt,
            ConversationId = conversation.Id,
            UserRole = "Shop",
            ClassifiedIntent = "business_advisor",
            ConversationHistory = new List<GroqMessageDto>
            {
                new() { Role = "user", Content = prompt }
            }
        };

        var result = await advisorAgent.ProcessAsync(context);

        if (!result.Success || string.IsNullOrWhiteSpace(result.ResponseText))
        {
            _logger.LogWarning("[BusinessAdvisorJobs] Agent returned empty response for store {StoreUserId}", storeUserId);
            return;
        }

        // Calculate health score for storage
        var healthScore = await advisorService.CalculateHealthScoreAsync(storeUserId);

        // Save the report
        var inspection = await advisorService.InspectStoreAsync(storeUserId);
        await advisorService.SaveReportAsync(
            storeUserId,
            reportType,
            result.ResponseText,
            healthScore.Score,
            JsonSerializer.Serialize(inspection));

        // Send notification via Telegram
        try
        {
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var notificationType = $"BusinessAdvisor{reportType}";
            var title = reportType switch
            {
                "Daily" => "📊 Daily Business Report",
                "Weekly" => "📈 Weekly Business Summary",
                "Monthly" => "🎯 Monthly Strategy Report",
                _ => "📊 Business Advisor Report"
            };

            // Truncate for Telegram (4096 char limit)
            var messageBody = result.ResponseText.Length > 3900
                ? result.ResponseText[..3900] + "\n\n[Report truncated. View full report in the app.]"
                : result.ResponseText;

            await telegramService.TrySendEventNotificationAsync(
                storeUserId, notificationType, $"{title}\n\n{messageBody}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BusinessAdvisorJobs] Failed to send Telegram notification for store {StoreUserId}", storeUserId);
        }

        _logger.LogInformation("[BusinessAdvisorJobs] Generated {ReportType} report for store {StoreUserId} (Health: {Score}/100)",
            reportType, storeUserId, healthScore.Score);
    }
}
