using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OnlineStore.Api.Services;

public class TelegramService : ITelegramService
{
    private readonly ApplicationDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private readonly ILogger<TelegramService> _logger;

    // All supported notification types
    public static readonly string[] AllNotificationTypes =
    {
        "NewCustomer", "NewOrder", "OrderDelivered",
        "LowStock", "NegativeReview",
        "DailyIncome", "WeeklyReport", "MonthlyReport",
        "RevenueGoal", "ReturningCustomer",
        "TopProducts", "AiInsights"
    };

    // Event-driven (real-time) types — not scheduled
    public static readonly HashSet<string> EventTypes = new()
    {
        "NewCustomer", "NewOrder", "OrderDelivered",
        "LowStock", "NegativeReview",
        "RevenueGoal", "ReturningCustomer"
    };

    public TelegramService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TelegramService> logger)
    {
        _db = db;
        _httpClient = httpClientFactory.CreateClient("TelegramBot");
        _botToken = config["Telegram:BotToken"] ?? "";
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    //  Connection
    // ═══════════════════════════════════════════════════

    public async Task<TelegramConnectionStatusDto> GetConnectionStatusAsync(string storeUserId)
    {
        var setting = await _db.StoreTelegramSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId);

        if (setting is null || !setting.BotConnected)
            return new TelegramConnectionStatusDto(false, null, null);

        return new TelegramConnectionStatusDto(
            true,
            MaskChatId(setting.ChatId),
            setting.ConnectedAtUtc
        );
    }

    public async Task<TelegramConnectionStatusDto> ConnectAsync(string storeUserId, long chatId)
    {
        // Validate by sending a test message
        try
        {
            await SendMessageAsync(chatId, "✅ LAGBAR Store Bot connected successfully!\n\nYou will receive notifications here.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram connection message to chat {ChatId}", chatId);
            throw new InvalidOperationException("Could not send message to the provided Telegram Chat ID. Make sure you have started the bot first.");
        }

        var existing = await _db.StoreTelegramSettings
            .AsTracking()
            .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId);

        if (existing is not null)
        {
            existing.ChatId = chatId;
            existing.BotConnected = true;
            existing.ConnectedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _db.StoreTelegramSettings.Add(new StoreTelegramSetting
            {
                StoreUserId = storeUserId,
                ChatId = chatId,
                BotConnected = true,
                ConnectedAtUtc = DateTime.UtcNow
            });
        }

        // Seed default notification settings if none exist
        var hasSettings = await _db.StoreTelegramNotificationSettings
            .AnyAsync(s => s.StoreUserId == storeUserId);

        if (!hasSettings)
        {
            foreach (var type in AllNotificationTypes)
            {
                _db.StoreTelegramNotificationSettings.Add(new StoreTelegramNotificationSetting
                {
                    StoreUserId = storeUserId,
                    NotificationType = type,
                    Enabled = false,
                    Frequency = EventTypes.Contains(type) ? "Instant" : "Daily",
                    Hour = 9,
                    Minute = 0,
                    TimeZone = "UTC"
                });
            }
        }

        await _db.SaveChangesAsync();

        return new TelegramConnectionStatusDto(true, MaskChatId(chatId), DateTime.UtcNow);
    }

    public async Task DisconnectAsync(string storeUserId)
    {
        var setting = await _db.StoreTelegramSettings
            .AsTracking()
            .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId);

        if (setting is not null)
        {
            setting.BotConnected = false;
            await _db.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════
    //  Notification Settings
    // ═══════════════════════════════════════════════════

    public async Task<List<TelegramNotificationSettingDto>> GetNotificationSettingsAsync(string storeUserId)
    {
        var settings = await _db.StoreTelegramNotificationSettings
            .AsNoTracking()
            .Where(s => s.StoreUserId == storeUserId)
            .OrderBy(s => s.Id)
            .ToListAsync();

        // If no settings exist yet, return defaults (all disabled)
        if (settings.Count == 0)
        {
            return AllNotificationTypes.Select(t => new TelegramNotificationSettingDto(
                t, false,
                EventTypes.Contains(t) ? "Instant" : "Daily",
                null, null, 9, 0, "UTC"
            )).ToList();
        }

        return settings.Select(s => new TelegramNotificationSettingDto(
            s.NotificationType,
            s.Enabled,
            s.Frequency,
            s.IntervalDays,
            s.WeeklyDay,
            s.Hour,
            s.Minute,
            s.TimeZone
        )).ToList();
    }

    public async Task UpdateNotificationSettingsAsync(string storeUserId, List<TelegramNotificationSettingDto> settings)
    {
        foreach (var dto in settings)
        {
            if (!AllNotificationTypes.Contains(dto.NotificationType))
                continue;

            var existing = await _db.StoreTelegramNotificationSettings
                .AsTracking()
                .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId && s.NotificationType == dto.NotificationType);

            if (existing is not null)
            {
                existing.Enabled = dto.Enabled;
                existing.Frequency = dto.Frequency;
                existing.IntervalDays = dto.IntervalDays;
                existing.WeeklyDay = dto.WeeklyDay;
                existing.Hour = dto.Hour;
                existing.Minute = dto.Minute;
                existing.TimeZone = dto.TimeZone;
            }
            else
            {
                _db.StoreTelegramNotificationSettings.Add(new StoreTelegramNotificationSetting
                {
                    StoreUserId = storeUserId,
                    NotificationType = dto.NotificationType,
                    Enabled = dto.Enabled,
                    Frequency = dto.Frequency,
                    IntervalDays = dto.IntervalDays,
                    WeeklyDay = dto.WeeklyDay,
                    Hour = dto.Hour,
                    Minute = dto.Minute,
                    TimeZone = dto.TimeZone
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════
    //  Messaging
    // ═══════════════════════════════════════════════════

    public async Task SendMessageAsync(long chatId, string message)
    {
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            _logger.LogWarning("Telegram BotToken is not configured. Message not sent.");
            return;
        }

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "HTML"
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Telegram API error: {Status} — {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Telegram API returned {response.StatusCode}");
        }
    }

    public async Task<bool> SendTestMessageAsync(string storeUserId)
    {
        var setting = await _db.StoreTelegramSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId && s.BotConnected);

        if (setting is null)
            return false;

        var shopName = await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => s.UserId == storeUserId)
            .Select(s => s.ShopName)
            .FirstOrDefaultAsync() ?? "Your Store";

        var message = $"🔔 <b>Test Notification</b>\n\n" +
                      $"Store: {shopName}\n" +
                      $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                      $"✅ Telegram notifications are working correctly!";

        await SendMessageAsync(setting.ChatId, message);
        return true;
    }

    // ═══════════════════════════════════════════════════
    //  Event-Driven Notifications
    // ═══════════════════════════════════════════════════

    public async Task TrySendEventNotificationAsync(string storeUserId, string notificationType, string message)
    {
        try
        {
            var connection = await _db.StoreTelegramSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId && s.BotConnected);

            if (connection is null) return;

            var setting = await _db.StoreTelegramNotificationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StoreUserId == storeUserId
                    && s.NotificationType == notificationType
                    && s.Enabled);

            if (setting is null) return;

            await SendMessageAsync(connection.ChatId, message);

            // Update last sent
            var tracked = await _db.StoreTelegramNotificationSettings
                .AsTracking()
                .FirstOrDefaultAsync(s => s.Id == setting.Id);
            if (tracked is not null)
            {
                tracked.LastSentAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram event notification {Type} for store {UserId}",
                notificationType, storeUserId);
        }
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    private static string MaskChatId(long chatId)
    {
        var str = chatId.ToString();
        if (str.Length <= 4)
            return new string('*', str.Length);

        return str[..2] + new string('*', str.Length - 4) + str[^2..];
    }
}
