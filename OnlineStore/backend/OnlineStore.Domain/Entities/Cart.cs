namespace OnlineStore.Domain.Entities;

public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
