using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Authorize(Roles = "Shop")]
public class AnalyticsController : ControllerBase
{
    private readonly Services.IAnalyticsService _analytics;
    private readonly Services.IReportExportService _reportExport;
    private readonly ApplicationDbContext _db;

    public AnalyticsController(
        Services.IAnalyticsService analytics,
        Services.IReportExportService reportExport,
        ApplicationDbContext db)
    {
        _analytics = analytics;
        _reportExport = reportExport;
        _db = db;
    }

    // ═══════════════════════════════════════════════════
    //  Dashboard — Full Analytics
    // ═══════════════════════════════════════════════════

    /// <summary>Get full analytics dashboard data for the logged-in store.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<FullAnalyticsDto>> GetDashboard(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var result = await _analytics.GetFullAnalyticsAsync(userId, startDate, endDate);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════
    //  Reports — JSON
    // ═══════════════════════════════════════════════════

    /// <summary>Get monthly report data.</summary>
    [HttpGet("reports/monthly")]
    public async Task<ActionResult<MonthlyReportDto>> GetMonthlyReport(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var now = DateTime.UtcNow;
        var reportYear = year ?? now.Year;
        var reportMonth = month ?? now.Month;

        if (reportMonth < 1 || reportMonth > 12)
            return BadRequest(new { error = "Month must be between 1 and 12" });

        var result = await _analytics.GetMonthlyReportAsync(userId, reportYear, reportMonth);
        return Ok(result);
    }

    /// <summary>Get yearly report data.</summary>
    [HttpGet("reports/yearly")]
    public async Task<ActionResult<YearlyReportDto>> GetYearlyReport([FromQuery] int? year)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var reportYear = year ?? DateTime.UtcNow.Year;
        var result = await _analytics.GetYearlyReportAsync(userId, reportYear);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════
    //  Reports — PDF Export
    // ═══════════════════════════════════════════════════

    /// <summary>Download monthly report as PDF.</summary>
    [HttpGet("reports/monthly/pdf")]
    public async Task<IActionResult> DownloadMonthlyPdf(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? lang)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var now = DateTime.UtcNow;
        var reportYear = year ?? now.Year;
        var reportMonth = month ?? now.Month;

        if (reportMonth < 1 || reportMonth > 12)
            return BadRequest(new { error = "Month must be between 1 and 12" });

        var shopName = await GetShopNameAsync(userId);
        var report = await _analytics.GetMonthlyReportAsync(userId, reportYear, reportMonth);
        var pdf = _reportExport.GenerateMonthlyPdf(report, shopName, lang ?? "en");

        return File(pdf, "application/pdf", $"MonthlySalesReport_{reportYear}_{reportMonth:D2}.pdf");
    }

    /// <summary>Download yearly report as PDF.</summary>
    [HttpGet("reports/yearly/pdf")]
    public async Task<IActionResult> DownloadYearlyPdf(
        [FromQuery] int? year,
        [FromQuery] string? lang)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var reportYear = year ?? DateTime.UtcNow.Year;
        var shopName = await GetShopNameAsync(userId);
        var report = await _analytics.GetYearlyReportAsync(userId, reportYear);
        var pdf = _reportExport.GenerateYearlyPdf(report, shopName, lang ?? "en");

        return File(pdf, "application/pdf", $"YearlySalesReport_{reportYear}.pdf");
    }

    // ═══════════════════════════════════════════════════
    //  Reports — Excel Export
    // ═══════════════════════════════════════════════════

    /// <summary>Download monthly report as Excel.</summary>
    [HttpGet("reports/monthly/excel")]
    public async Task<IActionResult> DownloadMonthlyExcel(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? lang)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var now = DateTime.UtcNow;
        var reportYear = year ?? now.Year;
        var reportMonth = month ?? now.Month;

        if (reportMonth < 1 || reportMonth > 12)
            return BadRequest(new { error = "Month must be between 1 and 12" });

        var shopName = await GetShopNameAsync(userId);
        var report = await _analytics.GetMonthlyReportAsync(userId, reportYear, reportMonth);
        var excel = _reportExport.GenerateMonthlyExcel(report, shopName, lang ?? "en");

        return File(excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"MonthlySalesReport_{reportYear}_{reportMonth:D2}.xlsx");
    }

    /// <summary>Download yearly report as Excel.</summary>
    [HttpGet("reports/yearly/excel")]
    public async Task<IActionResult> DownloadYearlyExcel(
        [FromQuery] int? year,
        [FromQuery] string? lang)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var reportYear = year ?? DateTime.UtcNow.Year;
        var shopName = await GetShopNameAsync(userId);
        var report = await _analytics.GetYearlyReportAsync(userId, reportYear);
        var excel = _reportExport.GenerateYearlyExcel(report, shopName, lang ?? "en");

        return File(excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"YearlySalesReport_{reportYear}.xlsx");
    }

    // ═══════════════════════════════════════════════════
    //  Settings
    // ═══════════════════════════════════════════════════

    /// <summary>Update the low stock threshold for this store.</summary>
    [HttpPut("settings/low-stock-threshold")]
    public async Task<IActionResult> UpdateLowStockThreshold(UpdateLowStockThresholdRequest req)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (req.Threshold < 1 || req.Threshold > 1000)
            return BadRequest(new { error = "Threshold must be between 1 and 1000" });

        var shop = await _db.ShopProfiles
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop is null) return NotFound(new { error = "Shop profile not found" });

        shop.LowStockThreshold = req.Threshold;
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse($"Low stock threshold updated to {req.Threshold}"));
    }

    // ═══════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════

    private async Task<string> GetShopNameAsync(string userId)
    {
        return await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.ShopName)
            .FirstOrDefaultAsync() ?? "Store";
    }
}
