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

    // Store Analytics Aggregations & Events
    public DbSet<StoreAnalyticsEvent> StoreAnalyticsEvents => Set<StoreAnalyticsEvent>();
    public DbSet<StoreAnalyticsDaily> StoreAnalyticsDailies => Set<StoreAnalyticsDaily>();
    public DbSet<StoreAnalyticsMonthly> StoreAnalyticsMonthlies => Set<StoreAnalyticsMonthly>();
    public DbSet<StoreAnalyticsYearly> StoreAnalyticsYearlies => Set<StoreAnalyticsYearly>();

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

        // Store Analytics Configurations
        builder.Entity<StoreAnalyticsEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => new { x.StoreUserId, x.CreatedAtUtc });
        });

        builder.Entity<StoreAnalyticsDaily>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
            b.Property(x => x.AverageOrderValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.StoreUserId, x.Date }).IsUnique();
            b.HasIndex(x => x.Date);
        });

        builder.Entity<StoreAnalyticsMonthly>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
            b.Property(x => x.AverageOrderValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.StoreUserId, x.Year, x.Month }).IsUnique();
        });

        builder.Entity<StoreAnalyticsYearly>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
            b.Property(x => x.AverageOrderValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.StoreUserId, x.Year }).IsUnique();
        });
    }

    private bool _isSavingAnalytics;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isSavingAnalytics)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var orderChanges = ChangeTracker.Entries<Order>()
            .Select(e => new { e.Entity, e.State, OriginalStatus = e.OriginalValues.GetValue<OrderStatus>(nameof(Order.Status)) })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        var eventsToAdd = new List<StoreAnalyticsEvent>();
        var orderIdsToRemoveEvents = new List<int>();

        foreach (var change in orderChanges)
        {
            var order = change.Entity;
            if (change.State == EntityState.Added && order.Status != OrderStatus.Cancelled)
            {
                var items = order.Items;
                if (items == null || items.Count == 0)
                {
                    items = await OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync(cancellationToken);
                }

                foreach (var item in items)
                {
                    eventsToAdd.Add(new StoreAnalyticsEvent
                    {
                        StoreUserId = order.StoreUserId,
                        UserId = order.UserId,
                        OrderId = order.Id,
                        ProductId = item.ProductId ?? 0,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.Quantity * item.UnitPrice,
                        CreatedAtUtc = order.CreatedAtUtc
                    });
                }
            }
            else if (change.State == EntityState.Modified && order.Status == OrderStatus.Cancelled && change.OriginalStatus != OrderStatus.Cancelled)
            {
                orderIdsToRemoveEvents.Add(order.Id);
            }
        }

        if (eventsToAdd.Count > 0 || orderIdsToRemoveEvents.Count > 0)
        {
            _isSavingAnalytics = true;
            try
            {
                if (orderIdsToRemoveEvents.Count > 0)
                {
                    await StoreAnalyticsEvents
                        .Where(e => orderIdsToRemoveEvents.Contains(e.OrderId))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                if (eventsToAdd.Count > 0)
                {
                    await StoreAnalyticsEvents.AddRangeAsync(eventsToAdd, cancellationToken);
                }

                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isSavingAnalytics = false;
            }
        }

        return result;
    }

    public override int SaveChanges()
    {
        if (_isSavingAnalytics)
        {
            return base.SaveChanges();
        }

        var orderChanges = ChangeTracker.Entries<Order>()
            .Select(e => new { e.Entity, e.State, OriginalStatus = e.OriginalValues.GetValue<OrderStatus>(nameof(Order.Status)) })
            .ToList();

        var result = base.SaveChanges();

        var eventsToAdd = new List<StoreAnalyticsEvent>();
        var orderIdsToRemoveEvents = new List<int>();

        foreach (var change in orderChanges)
        {
            var order = change.Entity;
            if (change.State == EntityState.Added && order.Status != OrderStatus.Cancelled)
            {
                var items = order.Items;
                if (items == null || items.Count == 0)
                {
                    items = OrderItems.Where(oi => oi.OrderId == order.Id).ToList();
                }

                foreach (var item in items)
                {
                    eventsToAdd.Add(new StoreAnalyticsEvent
                    {
                        StoreUserId = order.StoreUserId,
                        UserId = order.UserId,
                        OrderId = order.Id,
                        ProductId = item.ProductId ?? 0,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.Quantity * item.UnitPrice,
                        CreatedAtUtc = order.CreatedAtUtc
                    });
                }
            }
            else if (change.State == EntityState.Modified && order.Status == OrderStatus.Cancelled && change.OriginalStatus != OrderStatus.Cancelled)
            {
                orderIdsToRemoveEvents.Add(order.Id);
            }
        }

        if (eventsToAdd.Count > 0 || orderIdsToRemoveEvents.Count > 0)
        {
            _isSavingAnalytics = true;
            try
            {
                if (orderIdsToRemoveEvents.Count > 0)
                {
                    var ids = orderIdsToRemoveEvents;
                    var eventsToDelete = StoreAnalyticsEvents.Where(e => ids.Contains(e.OrderId)).ToList();
                    StoreAnalyticsEvents.RemoveRange(eventsToDelete);
                }

                if (eventsToAdd.Count > 0)
                {
                    StoreAnalyticsEvents.AddRange(eventsToAdd);
                }

                base.SaveChanges();
            }
            finally
            {
                _isSavingAnalytics = false;
            }
        }

        return result;
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
