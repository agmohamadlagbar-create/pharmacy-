namespace OnlineStore.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    /// <summary>The customer who placed the order.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>The store this order belongs to.</summary>
    public string StoreUserId { get; set; } = default!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? OfferCode { get; set; }
    public string? PaymentMethod { get; set; }

    public string? OrderGroupId { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledByRole { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? PreparingAtUtc { get; set; }
    public DateTime? ReadyForPickupAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
