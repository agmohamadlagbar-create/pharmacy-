using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<HealthController> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    /// <summary>Diagnostic endpoint to test FCM setup in production environment.</summary>
    [Authorize(Roles = "Admin,Shop")]
    [HttpGet("fcm")]
    public async Task<IActionResult> CheckFcmHealth()
    {
        var serviceAccountRelPath = _config["Firebase:ServiceAccountPath"] ?? "firebase-service-account.json";
        var fullPath = Path.IsPathRooted(serviceAccountRelPath)
            ? serviceAccountRelPath
            : Path.Combine(_env.ContentRootPath, serviceAccountRelPath);

        bool fileExists = System.IO.File.Exists(fullPath);
        long fileSize = fileExists ? new FileInfo(fullPath).Length : 0;

        int activeDeviceTokens = 0;
        try
        {
            activeDeviceTokens = await _db.UserDeviceTokens.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count user device tokens");
        }

        var projectId = _config["Firebase:ProjectId"] ?? "online-store-cbba7";

        return Ok(new
        {
            Status = fileExists ? "OK" : "MockMode_ServiceAccountMissing",
            Environment = _env.EnvironmentName,
            ContentRootPath = _env.ContentRootPath,
            ServiceAccountConfigPath = serviceAccountRelPath,
            ServiceAccountResolvedPath = fullPath,
            ServiceAccountFileExists = fileExists,
            ServiceAccountFileSize = fileSize,
            ProjectId = projectId,
            TotalRegisteredDeviceTokens = activeDeviceTokens,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
