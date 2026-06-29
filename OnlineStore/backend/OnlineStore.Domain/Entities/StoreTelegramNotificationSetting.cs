namespace OnlineStore.Domain.Entities;

/// <summary>
/// Per-notification-type scheduling configuration for a store's Telegram notifications.
/// </summary>
public class StoreTelegramNotificationSetting
{
    public int Id { get; set; }

    /// <summary>The shop owner's ASP.NET Identity user ID.</summary>
    public string StoreUserId { get; set; } = default!;

    /// <summary>
    /// Notification type key, e.g. "NewCustomer", "NewOrder", "OrderDelivered",
    /// "LowStock", "NegativeReview", "DailyIncome", "WeeklyReport",
    /// "MonthlyReport", "RevenueGoal", "ReturningCustomer", "TopProducts", "AiInsights".
    /// </summary>
    public string NotificationType { get; set; } = default!;

    public bool Enabled { get; set; }

    /// <summary>
    /// Frequency key: "Daily", "Every2Days", "Every3Days", "Weekly", "Monthly", "Custom".
    /// Only relevant for scheduled (non-event) notifications.
    /// </summary>
    public string Frequency { get; set; } = "Daily";

    /// <summary>Custom interval in days (used when Frequency == "Custom").</summary>
    public int? IntervalDays { get; set; }

    /// <summary>Day of week for weekly frequency (0 = Sunday … 6 = Saturday).</summary>
    public int? WeeklyDay { get; set; }

    /// <summary>Hour to send (0–23).</summary>
    public int Hour { get; set; } = 9;

    /// <summary>Minute to send (0–59).</summary>
    public int Minute { get; set; } = 0;

    /// <summary>IANA timezone ID, e.g. "Europe/Istanbul".</summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>When the last notification of this type was sent.</summary>
    public DateTime? LastSentAtUtc { get; set; }
}
