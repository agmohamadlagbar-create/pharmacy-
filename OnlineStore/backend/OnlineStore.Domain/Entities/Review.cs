namespace OnlineStore.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = default!;
    public string UserName { get; set; } = default!;
    
    public int Rating { get; set; } // 1 to 5
    public string? Comment { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product Product { get; set; } = default!;
}
