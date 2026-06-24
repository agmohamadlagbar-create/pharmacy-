using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using System.IO;
using System.Threading.Tasks;

namespace OnlineStore.Api.Helpers;

public static class ImageHelper
{
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] Riff = { 0x52, 0x49, 0x46, 0x46 }; // WebP starts with RIFF
    private static readonly byte[] Webp = { 0x57, 0x45, 0x42, 0x50 }; // WebP at offset 8

    public static async Task<(bool IsValid, string MimeType, string Error)> ValidateImageAsync(IFormFile file, long maxSizeBytes)
    {
        if (file == null || file.Length == 0)
            return (false, "", "No file uploaded.");

        if (file.Length > maxSizeBytes)
            return (false, "", $"File size exceeds {maxSizeBytes / (1024 * 1024)}MB limit.");

        byte[] header = new byte[32];
        using (var stream = file.OpenReadStream())
        {
            await stream.ReadAsync(header, 0, header.Length);
        }

        // JPEG
        if (StartsWith(header, Jpeg)) return (true, "image/jpeg", "");

        // PNG
        if (StartsWith(header, Png)) return (true, "image/png", "");

        // WebP
        if (StartsWith(header, Riff) && ContainsAt(header, 8, Webp)) return (true, "image/webp", "");

        // HEIF/HEIC
        if (IsHeif(header)) return (true, "image/heic", "");

        return (false, "", "Invalid image format. Supported: JPEG, PNG, WebP, HEIC/HEIF.");
    }

    private static bool StartsWith(byte[] header, byte[] pattern)
    {
        if (header.Length < pattern.Length) return false;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (header[i] != pattern[i]) return false;
        }
        return true;
    }

    private static bool ContainsAt(byte[] header, int offset, byte[] pattern)
    {
        if (header.Length < offset + pattern.Length) return false;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (header[offset + i] != pattern[i]) return false;
        }
        return true;
    }

    private static bool IsHeif(byte[] header)
    {
        // HEIF/HEIC magic bytes are usually at offset 4: 'ftypheic', 'ftypmif1', etc.
        if (header.Length < 12) return false;
        string brand = System.Text.Encoding.ASCII.GetString(header, 4, 8);
        return brand.StartsWith("ftyp") && (brand.Contains("heic") || brand.Contains("heif") || brand.Contains("mif1") || brand.Contains("msf1"));
    }

    public static bool IsReadable(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var info = Image.Identify(stream);
            return info != null;
        }
        catch
        {
            return false;
        }
    }
}
