namespace OnlineStore.Domain.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
