namespace OnlineStore.Api.Contracts;

// ════════════════════════════════════════════
//  Telegram — Connection
// ════════════════════════════════════════════

public record TelegramConnectionStatusDto(
    bool Connected,
    string? MaskedChatId,
    DateTime? ConnectedAt
);

public record TelegramConnectRequest(long ChatId);

// ════════════════════════════════════════════
//  Telegram — Notification Settings
// ════════════════════════════════════════════

public record TelegramNotificationSettingDto(
    string NotificationType,
    bool Enabled,
    string Frequency,
    int? IntervalDays,
    int? WeeklyDay,
    int Hour,
    int Minute,
    string TimeZone
);

public record UpdateTelegramSettingsRequest(
    List<TelegramNotificationSettingDto> Settings
);

// ════════════════════════════════════════════
//  Telegram — Responses
// ════════════════════════════════════════════

public record TelegramSettingsResponse(
    TelegramConnectionStatusDto Connection,
    List<TelegramNotificationSettingDto> Notifications
);
