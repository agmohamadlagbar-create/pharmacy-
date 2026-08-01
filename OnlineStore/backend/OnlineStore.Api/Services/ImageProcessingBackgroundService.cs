using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineStore.Api.Services;

public class ImageProcessingBackgroundService : BackgroundService
{
    private readonly IImageProcessingQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageProcessingBackgroundService> _logger;
    private readonly string _storageRoot;

    public ImageProcessingBackgroundService(
        IImageProcessingQueue queue,
        IServiceProvider serviceProvider,
        ILogger<ImageProcessingBackgroundService> logger,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;

        var configuredRoot = configuration["StorageRoot"] ?? "storage";
        _storageRoot = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(env.ContentRootPath, configuredRoot);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var item = await _queue.DequeueAsync(stoppingToken);
            _ = ProcessWithRetryAsync(item, 3);
        }
    }

    private async Task ProcessWithRetryAsync(ImageProcessingItem item, int maxRetries)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                await ProcessImageAsync(item);
                return;
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogError(ex, "Error processing image {StorageKey}. Attempt {Attempt} of {MaxRetries}.", item.StorageKey, attempt, maxRetries);
                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                }
            }
        }
    }

    private async Task ProcessImageAsync(ImageProcessingItem item)
    {
        string fullOriginalPath = Path.Combine(_storageRoot, item.StorageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullOriginalPath))
        {
            _logger.LogWarning("Original file not found: {Path}", fullOriginalPath);
            return;
        }

        using var image = await Image.LoadAsync(fullOriginalPath);
        
        // Define sizes
        var sizes = new[]
        {
            new { Name = "thumb", Width = 200, Height = 200, Method = "crop" },
            new { Name = "medium", Width = 800, Height = 0, Method = "resize" },
            new { Name = "large", Width = 1600, Height = 0, Method = "resize" }
        };

        var processedFolderRelative = Path.Combine(item.Country, item.City, item.EntityType, !string.IsNullOrEmpty(item.EntityId) ? item.EntityId : "", "processed");
        if (item.EntityType == "categories" || item.EntityType == "ads")
        {
             processedFolderRelative = Path.Combine(item.Country, item.City, item.EntityType, "processed");
        }
        
        string fullProcessedFolder = Path.Combine(_storageRoot, processedFolderRelative);
        Directory.CreateDirectory(fullProcessedFolder);

        var webpEncoder = new WebpEncoder { Quality = 82 };

        foreach (var size in sizes)
        {
            using var clone = image.Clone(x =>
            {
                if (size.Method == "crop")
                {
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(size.Width, size.Height),
                        Mode = ResizeMode.Crop
                    });
                }
                else
                {
                    x.Resize(size.Width, 0); // Preserve aspect ratio
                }
            });

            string processedFileName = $"{size.Name}_{Path.GetFileNameWithoutExtension(item.OriginalFileName)}.webp";
            string fullPath = Path.Combine(fullProcessedFolder, processedFileName);
            await clone.SaveAsync(fullPath, webpEncoder);

            string storageKey = Path.GetRelativePath(_storageRoot, fullPath).Replace('\\', '/');

            // Update Database
            await UpdateDatabaseAsync(item, size.Name, storageKey, processedFileName);
        }
    }

    private async Task UpdateDatabaseAsync(ImageProcessingItem item, string sizeName, string storageKey, string fileName)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IStoreCacheService>();

        if (item.EntityType == "products")
        {
            if (int.TryParse(item.EntityId, out int productId))
            {
                var existing = await db.ProductImages.FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.ImageType == sizeName && pi.FileName == fileName);
                if (existing == null)
                {
                    var original = await db.ProductImages.FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.ImageType == "original");

                    db.ProductImages.Add(new ProductImage
                    {
                        ProductId = productId,
                        Country = item.Country,
                        City = item.City,
                        StorageKey = storageKey,
                        FileName = fileName,
                        ImageType = sizeName,
                        UploadedBy = original?.UploadedBy ?? "system",
                        SortOrder = original?.SortOrder ?? 0
                    });
                }
                else
                {
                    existing.StorageKey = storageKey;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                // Rule: Never serve original if processed version exists.
                // Update the main Product.ImageUrl if it currently points to the original.
                var product = await db.Products.FindAsync(productId);
                if (product != null && sizeName == "large") // Using 'large' as the standard replacement for full-view
                {
                    if (string.IsNullOrEmpty(product.ImageUrl) || product.ImageUrl.Contains("/original/"))
                    {
                        product.ImageUrl = "/media/" + storageKey;
                    }
                }
            }
        }
        else if (item.EntityType == "users")
        {
            var existing = await db.UserImages.FirstOrDefaultAsync(ui => ui.UserId == item.EntityId && ui.ImageType == sizeName && ui.FileName == fileName);
            if (existing == null)
            {
                db.UserImages.Add(new UserImage
                {
                    UserId = item.EntityId,
                    Country = item.Country,
                    City = item.City,
                    StorageKey = storageKey,
                    FileName = fileName,
                    ImageType = sizeName,
                    Status = "active"
                });
            }
            else
            {
                existing.StorageKey = storageKey;
            }

            // Update user's ProfilePictureUrl
            var user = await db.Users.FindAsync(item.EntityId);
            if (user != null && sizeName == "medium") // Medium is usually good for profile pics
            {
                if (string.IsNullOrEmpty(user.ProfilePictureUrl) || user.ProfilePictureUrl.Contains("/original/"))
                {
                    user.ProfilePictureUrl = "/media/" + storageKey;
                }
            }
        }
        else if (item.EntityType == "categories")
        {
            if (sizeName == "large") // Using large as the replacement
            {
                var categories = await db.Categories
                    .Where(c => c.ImageUrl != null && c.ImageUrl.Contains(item.StorageKey))
                    .ToListAsync();

                foreach (var cat in categories)
                {
                    cat.ImageUrl = "/media/" + storageKey;
                }
            }
        }
        else if (item.EntityType == "ads")
        {
            if (sizeName == "large") // Using large as the replacement
            {
                var normalizedItemKey = item.StorageKey.Replace('\\', '/');
                var itemFileName = Path.GetFileName(normalizedItemKey);

                var ads = await db.AdBanners
                    .Where(a => a.ImageUrl != null)
                    .ToListAsync();

                var matchingAds = ads.Where(a => {
                    var normalizedAdUrl = a.ImageUrl!.Replace('\\', '/');
                    return normalizedAdUrl.Contains(normalizedItemKey) || 
                           (!string.IsNullOrEmpty(itemFileName) && normalizedAdUrl.Contains(itemFileName));
                }).ToList();

                var cleanStorageKey = storageKey.Replace('\\', '/');
                foreach (var ad in matchingAds)
                {
                    ad.ImageUrl = "/media/" + cleanStorageKey;
                }
            }
        }

        await db.SaveChangesAsync();

        // Invalidate relevant caches so the optimized URL is served immediately
        if (item.EntityType == "products") await cache.InvalidateProductsCacheAsync();
        else if (item.EntityType == "categories") await cache.InvalidateCategoriesCacheAsync();
        else if (item.EntityType == "ads") await cache.InvalidateAdsCacheAsync();
    }
}
