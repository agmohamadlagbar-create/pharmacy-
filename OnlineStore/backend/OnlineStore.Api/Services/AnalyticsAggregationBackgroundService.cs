using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Background service that runs hourly to:
/// 1. Perform daily, monthly, and yearly analytics aggregations.
/// 2. Purge raw analytics events older than 7 days that have been aggregated.
/// </summary>
public class AnalyticsAggregationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyticsAggregationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public AnalyticsAggregationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AnalyticsAggregationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsAggregationBackgroundService started. Running checks every hour.");

        // Wait 1 minute after startup before first run
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running analytics aggregation cycle...");
                await RunAggregationCycleAsync(stoppingToken);
                _logger.LogInformation("Analytics aggregation cycle completed.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in AnalyticsAggregationBackgroundService cycle.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    public async Task RunAggregationCycleAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        // 1. Find the last date aggregated in the database
        var lastAggregatedDate = await db.StoreAnalyticsDailies
            .AsNoTracking()
            .OrderByDescending(d => d.Date)
            .Select(d => (DateTime?)d.Date)
            .FirstOrDefaultAsync(ct);

        DateTime startDate;
        if (lastAggregatedDate.HasValue)
        {
            startDate = lastAggregatedDate.Value.AddDays(1);
        }
        else
        {
            // Find oldest raw event date
            var oldestEventDate = await db.StoreAnalyticsEvents
                .AsNoTracking()
                .OrderBy(e => e.CreatedAtUtc)
                .Select(e => (DateTime?)e.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            startDate = oldestEventDate?.Date ?? yesterday;
        }

        // 2. Perform daily catch-up aggregation
        if (startDate <= yesterday)
        {
            _logger.LogInformation("Aggregating daily analytics from {Start} to {End}...", startDate.ToString("yyyy-MM-dd"), yesterday.ToString("yyyy-MM-dd"));
            for (var date = startDate; date <= yesterday; date = date.AddDays(1))
            {
                await AggregateDailyAsync(db, date, ct);
                await AggregateMonthlyAsync(db, date.Year, date.Month, ct);
                await AggregateYearlyAsync(db, date.Year, ct);
            }
        }

        // 3. Purge raw events older than 7 days
        await PurgeExpiredEventsAsync(db, ct);
    }

    private async Task AggregateDailyAsync(ApplicationDbContext db, DateTime date, CancellationToken ct)
    {
        var start = date;
        var end = date.AddDays(1);

        // Fetch all raw events for this date
        var events = await db.StoreAnalyticsEvents
            .AsNoTracking()
            .Where(e => e.CreatedAtUtc >= start && e.CreatedAtUtc < end)
            .ToListAsync(ct);

        if (events.Count == 0)
        {
            _logger.LogInformation("No raw analytics events found for {Date}.", date.ToString("yyyy-MM-dd"));
            return;
        }

        // Group by StoreUserId
        var storeGroups = events.GroupBy(e => e.StoreUserId);

        foreach (var group in storeGroups)
        {
            var storeUserId = group.Key;
            var storeEvents = group.ToList();

            var totalSales = storeEvents.Sum(e => e.TotalAmount);
            var totalOrders = storeEvents.Select(e => e.OrderId).Distinct().Count();
            var avgOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0m;

            // Group by product for ProductSalesJson
            var productSales = storeEvents
                .GroupBy(e => e.ProductId)
                .Select(g => new ProductSalesDto
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(e => e.Quantity),
                    Revenue = g.Sum(e => e.TotalAmount)
                })
                .ToList();

            // Group by customer for CustomersJson
            var customerSales = storeEvents
                .GroupBy(e => e.UserId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.OrderId).Distinct().Count());

            // Save or update StoreAnalyticsDaily
            var dailyRecord = await db.StoreAnalyticsDailies
                .FirstOrDefaultAsync(d => d.StoreUserId == storeUserId && d.Date == date, ct);

            if (dailyRecord == null)
            {
                dailyRecord = new StoreAnalyticsDaily
                {
                    StoreUserId = storeUserId,
                    Date = date,
                    GeneratedAtUtc = DateTime.UtcNow
                };
                db.StoreAnalyticsDailies.Add(dailyRecord);
            }

            dailyRecord.TotalSales = totalSales;
            dailyRecord.TotalOrders = totalOrders;
            dailyRecord.AverageOrderValue = avgOrderValue;
            dailyRecord.ProductSalesJson = JsonSerializer.Serialize(productSales);
            dailyRecord.CustomersJson = JsonSerializer.Serialize(customerSales);
            dailyRecord.GeneratedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Daily aggregation completed for {Date}.", date.ToString("yyyy-MM-dd"));
    }

    private async Task AggregateMonthlyAsync(ApplicationDbContext db, int year, int month, CancellationToken ct)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        // Fetch daily records for this month
        var dailyRecords = await db.StoreAnalyticsDailies
            .AsNoTracking()
            .Where(d => d.Date >= start && d.Date < end)
            .ToListAsync(ct);

        if (dailyRecords.Count == 0) return;

        var storeGroups = dailyRecords.GroupBy(d => d.StoreUserId);

        foreach (var group in storeGroups)
        {
            var storeUserId = group.Key;
            var storeDailies = group.ToList();

            var totalSales = storeDailies.Sum(d => d.TotalSales);
            var totalOrders = storeDailies.Sum(d => d.TotalOrders);
            var avgOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0m;

            // Combine ProductSalesJson
            var productSalesDict = new Dictionary<int, ProductSalesDto>();
            foreach (var daily in storeDailies)
            {
                var dailyProducts = JsonSerializer.Deserialize<List<ProductSalesDto>>(daily.ProductSalesJson) ?? new();
                foreach (var p in dailyProducts)
                {
                    if (productSalesDict.TryGetValue(p.ProductId, out var existing))
                    {
                        existing.Quantity += p.Quantity;
                        existing.Revenue += p.Revenue;
                    }
                    else
                    {
                        productSalesDict[p.ProductId] = new ProductSalesDto
                        {
                            ProductId = p.ProductId,
                            Quantity = p.Quantity,
                            Revenue = p.Revenue
                        };
                    }
                }
            }

            // Combine CustomersJson
            var customerDict = new Dictionary<string, int>();
            foreach (var daily in storeDailies)
            {
                var dailyCustomers = JsonSerializer.Deserialize<Dictionary<string, int>>(daily.CustomersJson) ?? new();
                foreach (var c in dailyCustomers)
                {
                    if (customerDict.TryGetValue(c.Key, out var count))
                    {
                        customerDict[c.Key] = count + c.Value;
                    }
                    else
                    {
                        customerDict[c.Key] = c.Value;
                    }
                }
            }

            // Save or update StoreAnalyticsMonthly
            var monthlyRecord = await db.StoreAnalyticsMonthlies
                .FirstOrDefaultAsync(m => m.StoreUserId == storeUserId && m.Year == year && m.Month == month, ct);

            if (monthlyRecord == null)
            {
                monthlyRecord = new StoreAnalyticsMonthly
                {
                    StoreUserId = storeUserId,
                    Year = year,
                    Month = month,
                    GeneratedAtUtc = DateTime.UtcNow
                };
                db.StoreAnalyticsMonthlies.Add(monthlyRecord);
            }

            monthlyRecord.TotalSales = totalSales;
            monthlyRecord.TotalOrders = totalOrders;
            monthlyRecord.AverageOrderValue = avgOrderValue;
            monthlyRecord.ProductSalesJson = JsonSerializer.Serialize(productSalesDict.Values.ToList());
            monthlyRecord.CustomersJson = JsonSerializer.Serialize(customerDict);
            monthlyRecord.GeneratedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Monthly aggregation completed for {Year}-{Month:D2}.", year, month);
    }

    private async Task AggregateYearlyAsync(ApplicationDbContext db, int year, CancellationToken ct)
    {
        // Fetch monthly records for this year
        var monthlyRecords = await db.StoreAnalyticsMonthlies
            .AsNoTracking()
            .Where(m => m.Year == year)
            .ToListAsync(ct);

        if (monthlyRecords.Count == 0) return;

        var storeGroups = monthlyRecords.GroupBy(m => m.StoreUserId);

        foreach (var group in storeGroups)
        {
            var storeUserId = group.Key;
            var storeMonthlies = group.ToList();

            var totalSales = storeMonthlies.Sum(m => m.TotalSales);
            var totalOrders = storeMonthlies.Sum(m => m.TotalOrders);
            var avgOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0m;

            // Combine ProductSalesJson
            var productSalesDict = new Dictionary<int, ProductSalesDto>();
            foreach (var monthly in storeMonthlies)
            {
                var monthlyProducts = JsonSerializer.Deserialize<List<ProductSalesDto>>(monthly.ProductSalesJson) ?? new();
                foreach (var p in monthlyProducts)
                {
                    if (productSalesDict.TryGetValue(p.ProductId, out var existing))
                    {
                        existing.Quantity += p.Quantity;
                        existing.Revenue += p.Revenue;
                    }
                    else
                    {
                        productSalesDict[p.ProductId] = new ProductSalesDto
                        {
                            ProductId = p.ProductId,
                            Quantity = p.Quantity,
                            Revenue = p.Revenue
                        };
                    }
                }
            }

            // Combine CustomersJson
            var customerDict = new Dictionary<string, int>();
            foreach (var monthly in storeMonthlies)
            {
                var monthlyCustomers = JsonSerializer.Deserialize<Dictionary<string, int>>(monthly.CustomersJson) ?? new();
                foreach (var c in monthlyCustomers)
                {
                    if (customerDict.TryGetValue(c.Key, out var count))
                    {
                        customerDict[c.Key] = count + c.Value;
                    }
                    else
                    {
                        customerDict[c.Key] = c.Value;
                    }
                }
            }

            // Save or update StoreAnalyticsYearly
            var yearlyRecord = await db.StoreAnalyticsYearlies
                .FirstOrDefaultAsync(y => y.StoreUserId == storeUserId && y.Year == year, ct);

            if (yearlyRecord == null)
            {
                yearlyRecord = new StoreAnalyticsYearly
                {
                    StoreUserId = storeUserId,
                    Year = year,
                    GeneratedAtUtc = DateTime.UtcNow
                };
                db.StoreAnalyticsYearlies.Add(yearlyRecord);
            }

            yearlyRecord.TotalSales = totalSales;
            yearlyRecord.TotalOrders = totalOrders;
            yearlyRecord.AverageOrderValue = avgOrderValue;
            yearlyRecord.ProductSalesJson = JsonSerializer.Serialize(productSalesDict.Values.ToList());
            yearlyRecord.CustomersJson = JsonSerializer.Serialize(customerDict);
            yearlyRecord.GeneratedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Yearly aggregation completed for {Year}.", year);
    }

    private async Task PurgeExpiredEventsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-7);

        var deletedCount = await db.StoreAnalyticsEvents
            .Where(e => e.CreatedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Purged {Count} expired raw analytics events older than {Cutoff}.", deletedCount, cutoff.ToString("yyyy-MM-dd"));
        }
    }
}
