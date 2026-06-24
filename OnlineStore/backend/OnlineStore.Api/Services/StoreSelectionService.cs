using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class StoreSelectionService : IStoreSelectionService
{
    private readonly ApplicationDbContext _db;

    // Scoring weights — tunable for business priorities
    private const double DistanceWeight = 0.50;
    private const double PriceWeight = 0.35;
    private const double StockWeight = 0.15;

    public StoreSelectionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StoreSelectionResultDto> FindBestStoreAsync(
        string productName, int quantity, double customerLat, double customerLon)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return new StoreSelectionResultDto();

        var normalizedSearch = productName.ToLower();

        // 1. Query products with their stores, filtering by name, stock, and approval
        var rawData = await (from p in _db.Products
                             join s in _db.ShopProfiles on p.StoreUserId equals s.UserId
                             where p.Name.ToLower().Contains(normalizedSearch)
                                && p.StockQuantity >= quantity
                                && p.Status == ProductStatus.Approved
                                && s.Latitude != null
                                && s.Longitude != null
                             select new
                             {
                                 p.Id,
                                 p.Name,
                                 p.Price,
                                 p.StockQuantity,
                                 StoreId = s.UserId,
                                 StoreName = s.ShopName,
                                 s.StoreType,
                                 s.Location,
                                 Latitude = s.Latitude!.Value,
                                 Longitude = s.Longitude!.Value
                             }).ToListAsync();

        if (!rawData.Any())
            return new StoreSelectionResultDto();

        // 2. Calculate distances in memory using Haversine
        var options = rawData.Select(x => new StoreOptionDto
        {
            StoreId = x.StoreId,
            StoreName = x.StoreName,
            StoreType = x.StoreType,
            Location = x.Location,
            ProductId = x.Id,
            ProductName = x.Name,
            Price = x.Price,
            AvailableStock = x.StockQuantity,
            DistanceKm = Math.Round(HaversineKm(customerLat, customerLon, x.Latitude, x.Longitude), 2)
        }).ToList();

        // 3. Normalize metrics for scoring (min-max normalization)
        var minPrice = (double)options.Min(o => o.Price);
        var maxPrice = (double)options.Max(o => o.Price);
        var minDist = options.Min(o => o.DistanceKm);
        var maxDist = options.Max(o => o.DistanceKm);
        var maxStock = options.Max(o => o.AvailableStock);

        foreach (var opt in options)
        {
            double normDistance = (maxDist > minDist)
                ? (opt.DistanceKm - minDist) / (maxDist - minDist)
                : 0;

            double normPrice = (maxPrice > minPrice)
                ? ((double)opt.Price - minPrice) / (maxPrice - minPrice)
                : 0;

            // For stock, higher is better → invert normalization
            double normStock = maxStock > 0
                ? 1.0 - ((double)opt.AvailableStock / maxStock)
                : 0;

            // Lower score = better option
            opt.Score = Math.Round(
                (DistanceWeight * normDistance) +
                (PriceWeight * normPrice) +
                (StockWeight * normStock), 4);
        }

        // 4. Sort by composite score
        options = options.OrderBy(o => o.Score).ToList();

        return new StoreSelectionResultDto
        {
            BestOverall = options.First(),
            CheapestOption = options.OrderBy(o => o.Price).First(),
            NearestOption = options.OrderBy(o => o.DistanceKm).First(),
            AllOptions = options,
            TotalStoresFound = options.Count
        };
    }

    /// <summary>
    /// Haversine formula — returns distance in km between two geographic coordinates.
    /// </summary>
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => Math.PI * deg / 180.0;
}
