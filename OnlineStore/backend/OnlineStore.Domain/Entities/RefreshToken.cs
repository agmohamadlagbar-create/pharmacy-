namespace OnlineStore.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;

    /// <summary>SHA-256 hash of the actual token value.</summary>
    public string TokenHash { get; set; } = default!;

    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedUtc { get; set; }

    public bool IsRevoked => RevokedUtc.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public bool IsActive => !IsRevoked && !IsExpired;
}
