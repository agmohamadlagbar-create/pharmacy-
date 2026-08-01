using Microsoft.AspNetCore.Http;
using OnlineStore.Api.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnlineStore.Api.Services;

public interface IFileStorageService
{
    /// <summary>Save a product image under /{Country}/{City}/products/{productId}/original/</summary>
    Task<string> SaveProductImageAsync(string country, string city, int productId, string uploadedBy, IFormFile file);

    /// <summary>Save a user avatar under /{Country}/{City}/users/{userId}/avatar/original/</summary>
    Task<string> SaveUserAvatarAsync(string country, string city, string userId, IFormFile file);

    /// <summary>Save a category image under /{Country}/{City}/categories/</summary>
    Task<string> SaveCategoryImageAsync(string country, string city, IFormFile file);

    /// <summary>Save an ad banner image under /{Country}/{City}/ads/</summary>
    Task<string> SaveAdBannerImageAsync(string country, string city, IFormFile file);

    /// <summary>Archive (soft-delete) a file by moving it to /archive/{yyyy}/{MM}/</summary>
    void ArchiveFile(string storageKey);

    /// <summary>Get the full absolute path for a storage key.</summary>
    string GetFullPath(string storageKey);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly IImageProcessingQueue _imageQueue;
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public FileStorageService(IConfiguration configuration, IWebHostEnvironment env, IImageProcessingQueue imageQueue)
    {
        _imageQueue = imageQueue;
        var configuredRoot = configuration["StorageRoot"] ?? "storage";

        _storageRoot = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(env.ContentRootPath, configuredRoot);

        if (!Directory.Exists(_storageRoot))
            Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveProductImageAsync(string country, string city, int productId, string uploadedBy, IFormFile file)
    {
        await ValidateAndProcessStep1Step2(file);

        var sanitizedCountry = SanitizeLocation(country);
        var sanitizedCity = SanitizeLocation(city);

        var relativeFolder = Path.Combine(sanitizedCountry, sanitizedCity, "products", productId.ToString(), "original");
        var storageKey = await SaveCleanedOriginalAsync(relativeFolder, file);

        // Step 4: Queue background processing
        await _imageQueue.QueueImageWorkAsync(new ImageProcessingItem(
            storageKey, "products", productId.ToString(), sanitizedCountry, sanitizedCity, file.FileName));

        return storageKey;
    }

    public async Task<string> SaveUserAvatarAsync(string country, string city, string userId, IFormFile file)
    {
        await ValidateAndProcessStep1Step2(file);

        var sanitizedCountry = SanitizeLocation(country);
        var sanitizedCity = SanitizeLocation(city);

        var relativeFolder = Path.Combine(sanitizedCountry, sanitizedCity, "users", userId, "avatar", "original");
        var storageKey = await SaveCleanedOriginalAsync(relativeFolder, file);

        await _imageQueue.QueueImageWorkAsync(new ImageProcessingItem(
            storageKey, "users", userId, sanitizedCountry, sanitizedCity, file.FileName));

        return storageKey;
    }

    public async Task<string> SaveCategoryImageAsync(string country, string city, IFormFile file)
    {
        await ValidateAndProcessStep1Step2(file);

        var sanitizedCountry = SanitizeLocation(country);
        var sanitizedCity = SanitizeLocation(city);

        var relativeFolder = Path.Combine(sanitizedCountry, sanitizedCity, "categories");
        var storageKey = await SaveCleanedOriginalAsync(relativeFolder, file);

        await _imageQueue.QueueImageWorkAsync(new ImageProcessingItem(
            storageKey, "categories", "", sanitizedCountry, sanitizedCity, file.FileName));

        return storageKey;
    }

    public async Task<string> SaveAdBannerImageAsync(string country, string city, IFormFile file)
    {
        await ValidateAndProcessStep1Step2(file);

        var sanitizedCountry = SanitizeLocation(country);
        var sanitizedCity = SanitizeLocation(city);

        var relativeFolder = Path.Combine(sanitizedCountry, sanitizedCity, "ads");
        var storageKey = await SaveCleanedOriginalAsync(relativeFolder, file);
        storageKey = storageKey.Replace('\\', '/');

        await _imageQueue.QueueImageWorkAsync(new ImageProcessingItem(
            storageKey, "ads", "", sanitizedCountry, sanitizedCity, file.FileName));

        return storageKey;
    }

    public void ArchiveFile(string storageKey)
    {
        if (string.IsNullOrEmpty(storageKey)) return;

        var sourcePath = GetFullPath(storageKey);
        if (!File.Exists(sourcePath)) return;

        var now = DateTime.UtcNow;
        var archiveFolder = Path.Combine(_storageRoot, "archive", now.Year.ToString(), now.Month.ToString("D2"));
        Directory.CreateDirectory(archiveFolder);

        var fileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(archiveFolder, fileName);

        // Avoid name collision
        if (File.Exists(destPath))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            destPath = Path.Combine(archiveFolder, $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}");
        }

        try
        {
            File.Move(sourcePath, destPath);
        }
        catch (Exception ex)
        {
            // Log and ignore to prevent controller failure.
            // In a production app, we might want to use a formal logger here.
            Console.WriteLine($"[FileStorage] Failed to archive file {sourcePath}: {ex.Message}");
        }
    }

    public string GetFullPath(string storageKey)
    {
        // storage_key uses forward slashes; convert to OS path
        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_storageRoot, normalizedKey);
    }

    // ─────────────────── Private Helpers ───────────────────

    private async Task ValidateAndProcessStep1Step2(IFormFile file)
    {
        // Step 1: Validation
        var (isValid, mimeType, error) = await ImageHelper.ValidateImageAsync(file, MaxFileSizeBytes);
        if (!isValid) throw new ArgumentException(error, nameof(file));

        if (!ImageHelper.IsReadable(file))
            throw new ArgumentException("The uploaded file is corrupted or unreadable.", nameof(file));
    }

    /// <summary>
    /// Sanitize a location string: remove special chars, remove spaces, enforce PascalCase.
    /// "new york" → "NewYork", "São Paulo" → "SaoPaulo"
    /// </summary>
    private static string SanitizeLocation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        // Remove all non-letter characters except spaces (for word splitting)
        var lettersAndSpaces = Regex.Replace(value, @"[^a-zA-Z\s]", "");

        if (string.IsNullOrWhiteSpace(lettersAndSpaces))
            return "Unknown";

        // Split into words and apply PascalCase
        var words = lettersAndSpaces.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = string.Concat(words.Select(w =>
            char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1).ToLower() : "")
        ));

        return result;
    }

    /// <summary>
    /// Save the raw uploaded file directly to disk without processing.
    /// Image cleaning (auto-orient, EXIF strip) is handled by the background processing service.
    /// This keeps the HTTP request thread fast.
    /// </summary>
    private async Task<string> SaveCleanedOriginalAsync(string relativeFolder, IFormFile file)
    {
        var absoluteFolder = Path.Combine(_storageRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        var baseName = Path.GetFileNameWithoutExtension(file.FileName);
        baseName = Regex.Replace(baseName, @"[^a-zA-Z0-9_\-]", "_");
        var fileName = $"{baseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";

        var absolutePath = Path.Combine(absoluteFolder, fileName);
        if (File.Exists(absolutePath))
        {
            fileName = $"{baseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            absolutePath = Path.Combine(absoluteFolder, fileName);
        }

        // Save raw file directly — no ImageSharp processing on the request thread.
        // The background service will handle auto-orient, EXIF stripping, and resizing.
        await using (var sourceStream = file.OpenReadStream())
        await using (var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
        {
            await sourceStream.CopyToAsync(fileStream);
        }

        return Path.GetRelativePath(_storageRoot, absolutePath).Replace('\\', '/');
    }
}
