namespace OnlineStore.Domain.Entities;

/// <summary>
/// Cached shop-specific context so the Shop Owner AI doesn't re-query static data every conversation.
/// Includes business context, reporting preferences, and custom instructions.
/// </summary>
public class AiShopContext
{
    public int Id { get; set; }

    /// <summary>The shop owner's user ID.</summary>
    public string UserId { get; set; } = default!;

    public int ShopProfileId { get; set; }

    public string? PreferredLanguage { get; set; }
    public string? PreferredCurrency { get; set; }

    /// <summary>JSON: return policy, shipping info, business hours, etc.</summary>
    public string? ShopPoliciesJson { get; set; }

    /// <summary>JSON: preferred metrics, time formats, default date ranges, currency display.</summary>
    public string? ReportingPreferencesJson { get; set; }

    /// <summary>JSON: owner's custom instructions like "Always mention free shipping", "Use formal tone".</summary>
    public string? CustomInstructionsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
