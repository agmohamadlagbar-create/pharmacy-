namespace OnlineStore.Domain.Entities;

/// <summary>
/// Stores the Telegram Bot connection info for a shop owner.
/// Each shop can have one connected Telegram chat.
/// </summary>
public class StoreTelegramSetting
{
    public int Id { get; set; }

    /// <summary>The shop owner's ASP.NET Identity user ID.</summary>
    public string StoreUserId { get; set; } = default!;

    /// <summary>Telegram chat ID obtained when the owner starts the bot.</summary>
    public long ChatId { get; set; }

    /// <summary>Whether the bot connection is currently active.</summary>
    public bool BotConnected { get; set; }

    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
}
