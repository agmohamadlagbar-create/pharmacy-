using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IStoreCacheService _cache;

    public AdsController(ApplicationDbContext db, IFileStorageService fileStorage, IStoreCacheService cache)
    {
        _db = db;
        _fileStorage = fileStorage;
        _cache = cache;
    }

    /// <summary>Public – anyone can view ad banners.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AdBannerDto>>> GetAll()
    {
        var ads = await _cache.GetAdBannersAsync();
        return Ok(ads);
    }

    /// <summary>Admin only – upload ad banner image.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string? country, [FromQuery] string? city)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var c = country ?? "Unknown";
        var ci = city ?? "Unknown";

        try
        {
            var storageKey = await _fileStorage.SaveAdBannerImageAsync(c, ci, file);
            return Ok(new { imageUrl = "/media/" + storageKey });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin only – create ad banner.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AdBannerDto>> Create(CreateAdBannerRequest req)
    {
        var ad = new AdBanner
        {
            ImageUrl = req.ImageUrl,
            Title = req.Title,
            LinkUrl = req.LinkUrl,
            SortOrder = req.SortOrder ?? 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AdBanners.Add(ad);
        await _db.SaveChangesAsync();
        await _cache.InvalidateAdsCacheAsync();

        return CreatedAtAction(nameof(GetAll), null,
            new AdBannerDto(ad.Id, ad.ImageUrl, ad.Title, ad.LinkUrl, ad.SortOrder));
    }

    /// <summary>Admin only – delete ad banner and archive its image file.</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ad = await _db.AdBanners.FindAsync(id);
        if (ad is null) return NotFound();

        // Archive image instead of hard-deleting
        if (!string.IsNullOrEmpty(ad.ImageUrl))
        {
            // Robustly extract storage key by removing the /media/ prefix (case-insensitive)
            var storageKey = ad.ImageUrl;
            if (storageKey.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
            {
                storageKey = storageKey.Substring(7);
            }
            else if (storageKey.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
            {
                storageKey = storageKey.Substring(6);
            }

            try 
            {
                _fileStorage.ArchiveFile(storageKey);
            }
            catch (Exception ex)
            {
                // We proceed with DB deletion even if archival fails
                Console.WriteLine($"[AdsController] Warning: Could not archive ad image {ad.ImageUrl}: {ex.Message}");
            }
        }
        _db.AdBanners.Remove(ad);
        await _db.SaveChangesAsync();
        await _cache.InvalidateAdsCacheAsync();

        return NoContent();
    }
}
