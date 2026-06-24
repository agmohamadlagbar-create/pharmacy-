namespace OnlineStore.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Cart Cart { get; set; } = default!;
    public Product Product { get; set; } = default!;
}
