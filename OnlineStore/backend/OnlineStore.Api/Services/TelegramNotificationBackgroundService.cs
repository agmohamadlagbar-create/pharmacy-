using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Background service that runs every 60 seconds to check and dispatch
/// scheduled Telegram notifications (Daily Income, Weekly Report, etc.).
/// </summary>
public class TelegramNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramNotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

    // Only scheduled types — event types are handled in real-time
    private static readonly HashSet<string> ScheduledTypes = new()
    {
        "DailyIncome", "WeeklyReport", "MonthlyReport",
        "TopProducts", "AiInsights"
    };

    public TelegramNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TelegramNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelegramNotificationBackgroundService started. Checking every 60s.");

        // Wait 30 seconds after startup before first check
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledNotificationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing Telegram scheduled notifications.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessScheduledNotificationsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        var utcNow = DateTime.UtcNow;

        // Get all enabled scheduled notification settings
        var enabledSettings = await db.StoreTelegramNotificationSettings
            .AsTracking()
            .Where(s => s.Enabled && ScheduledTypes.Contains(s.NotificationType))
            .ToListAsync(ct);

        if (enabledSettings.Count == 0) return;

        // Get connected stores (only send if bot is connected)
        var connectedStoreUserIds = await db.StoreTelegramSettings
            .Where(s => s.BotConnected)
            .Select(s => s.StoreUserId)
            .ToListAsync(ct);

        var connectedSet = new HashSet<string>(connectedStoreUserIds);

        foreach (var setting in enabledSettings)
        {
            if (ct.IsCancellationRequested) break;
            if (!connectedSet.Contains(setting.StoreUserId)) continue;

            try
            {
                if (!IsDueToSend(setting, utcNow))
                    continue;

                var connection = await db.StoreTelegramSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StoreUserId == setting.StoreUserId && s.BotConnected, ct);

                if (connection is null) continue;

                var message = await GenerateReportMessageAsync(
                    setting.NotificationType, setting.StoreUserId,
                    analyticsService, db, ct);

                if (string.IsNullOrEmpty(message)) continue;

                await telegramService.SendMessageAsync(connection.ChatId, message);

                // Update LastSentAtUtc
                setting.LastSentAtUtc = utcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Sent Telegram {Type} to store {UserId}",
                    setting.NotificationType, setting.StoreUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send scheduled Telegram {Type} for store {UserId}",
                    setting.NotificationType, setting.StoreUserId);
            }
        }
    }

    // ═══════════════════════════════════════════════════
    //  Schedule Checking
    // ═══════════════════════════════════════════════════

    private static bool IsDueToSend(StoreTelegramNotificationSetting setting, DateTime utcNow)
    {
        // Convert UTC now to the user's timezone
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(setting.TimeZone);
        }
        catch
        {
            // Fallback: try IANA-to-Windows mapping or default to UTC
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(setting.TimeZone);
            }
            catch
            {
                tz = TimeZoneInfo.Utc;
            }
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        // Check if current hour:minute matches (within a 2-minute window)
        if (localNow.Hour != setting.Hour) return false;
        if (Math.Abs(localNow.Minute - setting.Minute) > 1) return false;

        // Check if already sent recently (within last hour to avoid duplicates)
        if (setting.LastSentAtUtc.HasValue)
        {
            var sinceLastSent = utcNow - setting.LastSentAtUtc.Value;
            if (sinceLastSent.TotalMinutes < 30) return false;
        }

        // Check frequency
        return setting.Frequency switch
        {
            "Daily" => true,
            "Every2Days" => IsDayIntervalMet(setting, utcNow, 2),
            "Every3Days" => IsDayIntervalMet(setting, utcNow, 3),
            "Weekly" => IsWeeklyDayMet(setting, localNow),
            "Monthly" => localNow.Day == 1, // Send on first day of month
            "Custom" => IsDayIntervalMet(setting, utcNow, setting.IntervalDays ?? 1),
            _ => false
        };
    }

    private static bool IsDayIntervalMet(StoreTelegramNotificationSetting setting, DateTime utcNow, int intervalDays)
    {
        if (!setting.LastSentAtUtc.HasValue)
            return true; // Never sent, send now

        var daysSinceLastSent = (utcNow - setting.LastSentAtUtc.Value).TotalDays;
        return daysSinceLastSent >= intervalDays - 0.1; // Small tolerance
    }

    private static bool IsWeeklyDayMet(StoreTelegramNotificationSetting setting, DateTime localNow)
    {
        var targetDay = setting.WeeklyDay ?? 0; // Default Sunday
        return (int)localNow.DayOfWeek == targetDay;
    }

    // ═══════════════════════════════════════════════════
    //  Report Message Generation
    // ═══════════════════════════════════════════════════

    private static async Task<string?> GenerateReportMessageAsync(
        string notificationType,
        string storeUserId,
        IAnalyticsService analyticsService,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var shopName = await db.ShopProfiles
            .AsNoTracking()
            .Where(s => s.UserId == storeUserId)
            .Select(s => s.ShopName)
            .FirstOrDefaultAsync(ct) ?? "Store";

        var currencySymbol = await GetCurrencySymbolAsync(db, storeUserId, ct);

        return notificationType switch
        {
            "DailyIncome" => await GenerateDailyIncomeAsync(storeUserId, shopName, currencySymbol, analyticsService),
            "WeeklyReport" => await GenerateWeeklyReportAsync(storeUserId, shopName, currencySymbol, analyticsService),
            "MonthlyReport" => await GenerateMonthlyReportAsync(storeUserId, shopName, currencySymbol, analyticsService),
            "TopProducts" => await GenerateTopProductsAsync(storeUserId, shopName, currencySymbol, analyticsService),
            "AiInsights" => await GenerateAiInsightsAsync(storeUserId, shopName, currencySymbol, analyticsService),
            _ => null
        };
    }

    private static async Task<string> GenerateDailyIncomeAsync(
        string storeUserId, string shopName, string currency, IAnalyticsService analytics)
    {
        var today = DateTime.UtcNow.Date;
        var data = await analytics.GetFullAnalyticsAsync(storeUserId, today, DateTime.UtcNow);

        var revenue = data.Summary.TotalRevenue;
        var orders = data.Summary.TotalOrders;
        var bestSeller = data.TopSellingProducts.FirstOrDefault();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📊 <b>Daily Income Report</b>");
        sb.AppendLine($"📍 {shopName}");
        sb.AppendLine();
        sb.AppendLine($"💰 Revenue: {currency}{revenue:N0}");
        sb.AppendLine($"📦 Orders: {orders}");
        if (bestSeller is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"🏆 Best Seller:");
            sb.AppendLine($"   {bestSeller.ProductName} ({bestSeller.QuantitySold} sold)");
        }
        sb.AppendLine();
        sb.AppendLine($"📅 {today:yyyy-MM-dd}");

        return sb.ToString();
    }

    private static async Task<string> GenerateWeeklyReportAsync(
        string storeUserId, string shopName, string currency, IAnalyticsService analytics)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);
        var data = await analytics.GetFullAnalyticsAsync(storeUserId, startDate, endDate);

        var revenue = data.Summary.TotalRevenue;
        var orders = data.Summary.TotalOrders;
        var growth = data.Growth.RevenueGrowthPercent;
        var topProduct = data.TopSellingProducts.FirstOrDefault();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📈 <b>Weekly Report</b>");
        sb.AppendLine($"📍 {shopName}");
        sb.AppendLine();
        sb.AppendLine($"💰 Revenue: {currency}{revenue:N0}");
        sb.AppendLine($"📦 Orders: {orders}");
        if (topProduct is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"🏆 Top Product:");
            sb.AppendLine($"   {topProduct.ProductName} ({topProduct.QuantitySold} sold)");
        }
        sb.AppendLine();
        var growthEmoji = growth >= 0 ? "📈" : "📉";
        sb.AppendLine($"{growthEmoji} Growth: {(growth >= 0 ? "+" : "")}{growth:N1}%");
        sb.AppendLine();
        sb.AppendLine($"📅 {startDate:MMM dd} – {endDate:MMM dd, yyyy}");

        return sb.ToString();
    }

    private static async Task<string> GenerateMonthlyReportAsync(
        string storeUserId, string shopName, string currency, IAnalyticsService analytics)
    {
        var now = DateTime.UtcNow;
        var report = await analytics.GetMonthlyReportAsync(storeUserId, now.Year, now.Month);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📅 <b>Monthly Report</b>");
        sb.AppendLine($"📍 {shopName}");
        sb.AppendLine();
        sb.AppendLine($"💰 Revenue: {currency}{report.TotalRevenue:N0}");
        sb.AppendLine($"📦 Orders: {report.TotalOrders}");
        sb.AppendLine($"💳 Avg Order: {currency}{report.AverageOrderValue:N0}");
        var topProduct = report.TopProducts.FirstOrDefault();
        if (topProduct is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"🏆 Best Product:");
            sb.AppendLine($"   {topProduct.ProductName}");
        }
        sb.AppendLine();
        sb.AppendLine($"📅 {report.MonthName} {report.Year}");

        return sb.ToString();
    }

    private static async Task<string> GenerateTopProductsAsync(
        string storeUserId, string shopName, string currency, IAnalyticsService analytics)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);
        var data = await analytics.GetFullAnalyticsAsync(storeUserId, startDate, endDate);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🏆 <b>Top Selling Products</b>");
        sb.AppendLine($"📍 {shopName}");
        sb.AppendLine();

        var top = data.TopSellingProducts.Take(5).ToList();
        if (top.Count == 0)
        {
            sb.AppendLine("No sales data for this period.");
        }
        else
        {
            for (int i = 0; i < top.Count; i++)
            {
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"#{i + 1}" };
                sb.AppendLine($"{medal} {top[i].ProductName}");
                sb.AppendLine($"   {top[i].QuantitySold} sold · {currency}{top[i].Revenue:N0}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"📅 Last 7 days");

        return sb.ToString();
    }

    private static async Task<string> GenerateAiInsightsAsync(
        string storeUserId, string shopName, string currency, IAnalyticsService analytics)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);
        var data = await analytics.GetFullAnalyticsAsync(storeUserId, startDate, endDate);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🤖 <b>AI Business Insights</b>");
        sb.AppendLine($"📍 {shopName}");
        sb.AppendLine();

        if (data.AiRecommendations.Count > 0)
        {
            foreach (var rec in data.AiRecommendations.Take(5))
            {
                var emoji = rec.Type switch
                {
                    "sales_up" => "📈",
                    "sales_down" => "📉",
                    "low_stock" => "⚠️",
                    "rating_high" => "⭐",
                    "rating_low" => "👎",
                    _ => "💡"
                };
                sb.AppendLine($"{emoji} {rec.Message}");
            }
        }
        else
        {
            // Generate basic insights from data
            if (data.Growth.RevenueGrowthPercent > 10)
                sb.AppendLine($"📈 Revenue is up {data.Growth.RevenueGrowthPercent:N1}% — great momentum!");
            else if (data.Growth.RevenueGrowthPercent < -10)
                sb.AppendLine($"📉 Revenue declined {Math.Abs(data.Growth.RevenueGrowthPercent):N1}%. Consider running promotions.");
            else
                sb.AppendLine($"📊 Revenue is stable ({data.Growth.RevenueGrowthPercent:N1}% change).");

            if (data.LowStockProducts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ {data.LowStockProducts.Count} product(s) running low on stock.");
            }

            var trending = data.TrendingProducts.FirstOrDefault();
            if (trending is not null && trending.GrowthPercent > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"🔥 {trending.ProductName} is trending (+{trending.GrowthPercent:N0}%).");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"📅 {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════
    //  Currency Helper
    // ═══════════════════════════════════════════════════

    private static async Task<string> GetCurrencySymbolAsync(
        ApplicationDbContext db, string storeUserId, CancellationToken ct)
    {
        var regionCode = await db.ShopProfiles
            .AsNoTracking()
            .Where(s => s.UserId == storeUserId)
            .Select(s => s.RegionCode)
            .FirstOrDefaultAsync(ct);

        // Use the currency symbol from analytics (which maps region→currency)
        // For simplicity, return common symbols
        return regionCode switch
        {
            "TR" or "TRY" => "₺",
            "US" or "USD" or "USA" => "$",
            "EU" or "EUR" => "€",
            "GB" or "GBP" or "UK" => "£",
            "IQ" or "IQD" => "IQD ",
            _ => "$"
        };
    }
}
