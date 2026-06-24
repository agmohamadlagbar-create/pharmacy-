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
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IStoreCacheService _cache;

    public CategoriesController(ApplicationDbContext db, IFileStorageService fileStorage, IStoreCacheService cache)
    {
        _db = db;
        _fileStorage = fileStorage;
        _cache = cache;
    }

    /// <summary>Public – anyone (including Guest) can view top-level categories. Supports optional pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 0, [FromQuery] int limit = 0)
    {
        var cats = await _cache.GetTopLevelCategoriesAsync();
        // When page/limit are provided, return paginated response (for admin)
        if (page > 0 && limit > 0)
            return Ok(PaginatedResponse<CategoryDto>.Create(cats, page, limit));
        // Otherwise return full list (for storefront)
        return Ok(cats);
    }

    /// <summary>Public – Get subcategories for a given parent category.</summary>
    [HttpGet("{id:int}/subcategories")]
    public async Task<ActionResult<List<CategoryDto>>> GetSubcategories(int id)
    {
        var cats = await _cache.GetSubcategoriesAsync(id);
        return Ok(cats);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var c = await _cache.GetCategoryByIdAsync(id);
        if (c is null) return NotFound();
        return Ok(c);
    }

    /// <summary>Admin only – create category.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest req)
    {
        var code = string.IsNullOrWhiteSpace(req.Code)
            ? req.Name.Trim().ToUpperInvariant().Replace(" ", "_")
            : req.Code.Trim().ToUpperInvariant();

        var cat = new Category { Name = req.Name, Code = code, ImageUrl = req.ImageUrl, ParentId = req.ParentId };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        await _cache.InvalidateCategoriesCacheAsync();

        return CreatedAtAction(nameof(GetById), new { id = cat.Id }, new CategoryDto(cat.Id, cat.Name, cat.Code, cat.ImageUrl, cat.ParentId));
    }

    /// <summary>Admin only – upload category image.</summary>
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
            var storageKey = await _fileStorage.SaveCategoryImageAsync(c, ci, file);
            return Ok(new { imageUrl = "/media/" + storageKey });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin only – update category.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest req)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        cat.Name = req.Name;
        cat.Code = string.IsNullOrWhiteSpace(req.Code)
            ? req.Name.Trim().ToUpperInvariant().Replace(" ", "_")
            : req.Code.Trim().ToUpperInvariant();
        cat.ImageUrl = req.ImageUrl;
        cat.ParentId = req.ParentId;
        await _db.SaveChangesAsync();

        await _cache.InvalidateCategoriesCacheAsync();

        return Ok(new CategoryDto(cat.Id, cat.Name, cat.Code, cat.ImageUrl, cat.ParentId));
    }

    /// <summary>Admin only – delete category.</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();

        await _cache.InvalidateCategoriesCacheAsync();

        return NoContent();
    }
}
