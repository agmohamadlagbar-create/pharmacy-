namespace OnlineStore.Api.Contracts;

public class OrderDetailsDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SupportOrderItemDto> Items { get; set; } = new();
}

public class SupportOrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class OrderTrackingDto
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class DeliveryEtaDto
{
    public int OrderId { get; set; }
    public string EstimatedDeliveryTime { get; set; } = string.Empty;
    public bool IsDelivered { get; set; }
}

public class RefundStatusDto
{
    public int OrderId { get; set; }
    public bool IsRefunded { get; set; }
    public string RefundStatusMessage { get; set; } = string.Empty;
}
