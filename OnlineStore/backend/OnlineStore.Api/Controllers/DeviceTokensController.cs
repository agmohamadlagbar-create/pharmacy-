using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeviceTokensController : ControllerBase
{
    private readonly IFcmService _fcmService;

    public DeviceTokensController(IFcmService fcmService)
    {
        _fcmService = fcmService;
    }

    public record RegisterDeviceTokenDto(string Token, string Platform);

    [HttpPost]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceTokenDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest(new { Message = "Token is required." });

        await _fcmService.RegisterDeviceTokenAsync(userId, dto.Token, dto.Platform ?? "Web");
        return Ok(new { Success = true, Message = "Device token registered successfully." });
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> UnregisterToken(string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { Message = "Token is required." });

        await _fcmService.UnregisterDeviceTokenAsync(userId, token);
        return Ok(new { Success = true, Message = "Device token unregistered successfully." });
    }
}
