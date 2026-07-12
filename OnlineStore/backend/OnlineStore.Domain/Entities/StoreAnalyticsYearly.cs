using System;

namespace OnlineStore.Domain.Entities;

public class StoreAnalyticsYearly
{
    public int Id { get; set; }
    public string StoreUserId { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    
    // Serialized list of { ProductId, Quantity, Revenue }
    public string ProductSalesJson { get; set; } = "[]";
    
    // Serialized dictionary of { UserId: OrderCount } for returning customers
    public string CustomersJson { get; set; } = "{}";
    
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
