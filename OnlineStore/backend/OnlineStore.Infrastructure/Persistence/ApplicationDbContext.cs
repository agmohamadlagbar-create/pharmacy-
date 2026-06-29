using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using System.Text.Json;

namespace OnlineStore.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ShopProfile> ShopProfiles => Set<ShopProfile>();
    public DbSet<DeliveryProfile> DeliveryProfiles => Set<DeliveryProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DeliveryJob> DeliveryJobs => Set<DeliveryJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AdBanner> AdBanners => Set<AdBanner>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<UserImage> UserImages => Set<UserImage>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiToolCall> AiToolCalls => Set<AiToolCall>();
    public DbSet<StoreTelegramSetting> StoreTelegramSettings => Set<StoreTelegramSetting>();
    public DbSet<StoreTelegramNotificationSetting> StoreTelegramNotificationSettings => Set<StoreTelegramNotificationSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ShopProfile
        builder.Entity<ShopProfile>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Location).HasMaxLength(400).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.OwnerName).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasIndex(x => x.UserId).IsUnique();
            b.Property(x => x.LowStockThreshold).HasDefaultValue(10);
        });

        // DeliveryProfile
        builder.Entity<DeliveryProfile>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.VehicleInfo).HasMaxLength(200);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        // Category
        builder.Entity<Category>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.ImageUrl).HasMaxLength(500);
            b.HasIndex(x => x.Name).IsUnique();
            b.HasIndex(x => x.ParentId);
        });

        // Product
        builder.Entity<Product>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.Property(x => x.ImageUrl).HasMaxLength(500);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);

            b.Property(x => x.AdditionalImageUrls)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => SafeDeserializeAdditionalImageUrls(v)
                );

            b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.Price);
        });

        // Review
        builder.Entity<Review>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UserName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Comment).HasMaxLength(1500);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.UserId);
        });

        // Order
        builder.Entity<Order>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.DeliveryAddress).HasMaxLength(500);
            b.Property(x => x.OrderGroupId).HasMaxLength(100);
            b.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => x.OrderGroupId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatedAtUtc);
        });

        // OrderItem
        builder.Entity<OrderItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.OrderId);
        });

        // DeliveryJob
        builder.Entity<DeliveryJob>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.DeliveryUserId).HasMaxLength(450);
            b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.OrderId).IsUnique();
            b.HasIndex(x => x.DeliveryUserId);
            b.HasIndex(x => x.Status);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TokenHash);
        });

        // Notification
        builder.Entity<Notification>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.IsRead });
            b.HasIndex(x => x.CreatedAtUtc);
        });

        // AdBanner
        builder.Entity<AdBanner>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.LinkUrl).HasMaxLength(500);
        });

        // Cart
        builder.Entity<Cart>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.HasMany(x => x.Items).WithOne(x => x.Cart).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        // CartItem
        builder.Entity<CartItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // ProductImage
        builder.Entity<ProductImage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Country).HasMaxLength(100).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired();
            b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ImageType).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            b.Property(x => x.UploadedBy).HasMaxLength(450).IsRequired();
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.StorageKey);
        });

        // UserImage
        builder.Entity<UserImage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Country).HasMaxLength(100).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired();
            b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ImageType).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.StorageKey);
        });

        // UserAddress
        builder.Entity<UserAddress>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(150);
            b.Property(x => x.Street).HasMaxLength(250);
            b.Property(x => x.BuildingNumber).HasMaxLength(50);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.HasIndex(x => x.UserId);
        });

        // AiConversation
        builder.Entity<AiConversation>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300);
            b.Property(x => x.ModelName).HasMaxLength(120);
            b.Property(x => x.SystemPrompt).HasMaxLength(8000);
            b.HasMany(x => x.Messages).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ToolCalls).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.OwnsOne(x => x.State, sb =>
            {
                sb.Property(s => s.PendingProductId);
                sb.Property(s => s.PendingProductName).HasMaxLength(300);
                sb.Property(s => s.PendingQuantity);
                sb.Property(s => s.PendingAction).HasMaxLength(100);
                sb.Property(s => s.AwaitingConfirmation);
            });
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.UpdatedAtUtc });
        });

        // AiMessage
        builder.Entity<AiMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.SequenceNumber).IsRequired();
            b.Property(x => x.Role).HasMaxLength(20).IsRequired();
            b.Property(x => x.Content).IsRequired();
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.ConversationId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.ConversationId, x.SequenceNumber }).IsUnique();
            b.HasIndex(x => new { x.ConversationId, x.UserId });
            b.ToTable(t => t.HasCheckConstraint("CK_AiMessages_Role", "[Role] IN ('user','assistant','system','tool')"));
        });

        // AiToolCall
        builder.Entity<AiToolCall>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ToolCallId).HasMaxLength(120).IsRequired();
            b.Property(x => x.ToolName).HasMaxLength(120).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(2000);
            b.HasOne(x => x.Message).WithMany(x => x.ToolCalls).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(x => x.ConversationId);
            b.HasIndex(x => x.MessageId);
            b.HasIndex(x => new { x.ConversationId, x.StartedAtUtc });
            b.HasIndex(x => new { x.ConversationId, x.ToolCallId }).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_AiToolCalls_Status", "[Status] IN ('Requested','Succeeded','Failed')"));
        });

        // StoreTelegramSetting
        builder.Entity<StoreTelegramSetting>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.HasIndex(x => x.StoreUserId).IsUnique();
        });

        // StoreTelegramNotificationSetting
        builder.Entity<StoreTelegramNotificationSetting>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.NotificationType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Frequency).HasMaxLength(50).HasDefaultValue("Daily");
            b.Property(x => x.TimeZone).HasMaxLength(100).HasDefaultValue("UTC");
            b.Property(x => x.Hour).HasDefaultValue(9);
            b.Property(x => x.Minute).HasDefaultValue(0);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => new { x.StoreUserId, x.NotificationType }).IsUnique();
            b.HasIndex(x => new { x.Enabled, x.NotificationType });
        });
    }

    private static List<string> SafeDeserializeAdditionalImageUrls(string? v)
    {
        if (string.IsNullOrWhiteSpace(v) || v.Length < 2)
            return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
