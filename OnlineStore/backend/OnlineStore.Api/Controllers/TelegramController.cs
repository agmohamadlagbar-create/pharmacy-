using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Authorize(Roles = "Shop")]
public class TelegramController : ControllerBase
{
    private readonly Services.ITelegramService _telegram;

    public TelegramController(Services.ITelegramService telegram)
    {
        _telegram = telegram;
    }

    // ═══════════════════════════════════════════════════
    //  Connection
    // ═══════════════════════════════════════════════════

    /// <summary>Get the Telegram connection status for the logged-in store.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<TelegramConnectionStatusDto>> GetStatus()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var result = await _telegram.GetConnectionStatusAsync(userId);
        return Ok(result);
    }

    /// <summary>Connect a Telegram bot using a chat ID.</summary>
    [HttpPost("connect")]
    public async Task<ActionResult<TelegramConnectionStatusDto>> Connect([FromBody] TelegramConnectRequest request)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (request.ChatId == 0)
            return BadRequest(new { error = "Chat ID is required." });

        try
        {
            var result = await _telegram.ConnectAsync(userId, request.ChatId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Disconnect the Telegram bot.</summary>
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        await _telegram.DisconnectAsync(userId);
        return Ok(new MessageResponse("Telegram bot disconnected."));
    }

    // ═══════════════════════════════════════════════════
    //  Notification Settings
    // ═══════════════════════════════════════════════════

    /// <summary>Get all notification settings for the logged-in store.</summary>
    [HttpGet("settings")]
    public async Task<ActionResult<TelegramSettingsResponse>> GetSettings()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var connection = await _telegram.GetConnectionStatusAsync(userId);
        var notifications = await _telegram.GetNotificationSettingsAsync(userId);

        return Ok(new TelegramSettingsResponse(connection, notifications));
    }

    /// <summary>Update notification settings for the logged-in store.</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTelegramSettingsRequest request)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        if (request.Settings is null || request.Settings.Count == 0)
            return BadRequest(new { error = "At least one setting is required." });

        await _telegram.UpdateNotificationSettingsAsync(userId, request.Settings);
        return Ok(new MessageResponse("Notification settings updated."));
    }

    // ═══════════════════════════════════════════════════
    //  Test Message
    // ═══════════════════════════════════════════════════

    /// <summary>Send a test Telegram message to the connected bot.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTest()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId is null) return Unauthorized();

        var sent = await _telegram.SendTestMessageAsync(userId);
        if (!sent)
            return BadRequest(new { error = "Telegram bot is not connected. Please connect first." });

        return Ok(new MessageResponse("Test message sent successfully!"));
    }
}
