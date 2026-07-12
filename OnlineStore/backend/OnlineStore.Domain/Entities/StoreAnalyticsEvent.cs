using System;

namespace OnlineStore.Domain.Entities;

public class StoreAnalyticsEvent
{
    public int Id { get; set; }
    public string StoreUserId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty; // Customer UserId for returning customers
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
