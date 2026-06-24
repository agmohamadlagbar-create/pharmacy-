using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly ApplicationDbContext _context;

    public ProductSearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductSearchSummaryDto> SearchProductsAsync(string productName, double userLat, double userLon)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return new ProductSearchSummaryDto();
        }

        // 1. Fetch available products matching the name
        // Using EF.Functions.Like for index-friendly case-insensitive search
        var searchPattern = $"%{productName}%";
        var query = await (from p in _context.Products.AsNoTracking()
                           join s in _context.ShopProfiles on p.StoreUserId equals s.UserId
                           where EF.Functions.Like(p.Name, searchPattern) 
                              && p.StockQuantity > 0
                              && p.Status == ProductStatus.Approved
                           select new
                           {
                               p.Id,
                               p.Name,
                               p.Price,
                               p.StockQuantity,
                               StoreId = s.UserId,
                               StoreName = s.ShopName,
                               s.Latitude,
                               s.Longitude
                           }).ToListAsync();

        if (!query.Any())
        {
            return new ProductSearchSummaryDto();
        }

        // 2. Map and calculate distances in memory
        var results = query.Select(x => new ProductSearchResultDto
        {
            ProductId = x.Id,
            ProductName = x.Name,
            Price = x.Price,
            Stock = x.StockQuantity,
            StoreId = x.StoreId,
            StoreName = x.StoreName,
            DistanceKm = (x.Latitude.HasValue && x.Longitude.HasValue) 
                ? CalculateDistanceKm(userLat, userLon, x.Latitude.Value, x.Longitude.Value) 
                : null
        }).ToList();

        // 3. Determine best price and nearest store
        var bestPrice = results.OrderBy(x => x.Price).FirstOrDefault();
        var nearestStore = results.Where(x => x.DistanceKm.HasValue)
                                  .OrderBy(x => x.DistanceKm)
                                  .FirstOrDefault();

        return new ProductSearchSummaryDto
        {
            AllAvailable = results,
            BestPrice = bestPrice,
            NearestStore = nearestStore
        };
    }

    // Haversine formula to calculate distance between two points on Earth
    private double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371d; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * 
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}
