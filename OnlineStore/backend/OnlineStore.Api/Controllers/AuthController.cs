using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailService _email;
    private readonly IVerificationCodeStore _codeStore;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ApplicationDbContext db,
        IJwtTokenService jwt,
        IEmailService email,
        IVerificationCodeStore codeStore)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _jwt = jwt;
        _email = email;
        _codeStore = codeStore;
    }

    // ──────────────── Pre-registration: send verification code ────────────────

    [HttpPost("send-code")]
    public async Task<IActionResult> SendCode(SendCodeRequest req)
    {
        var code = _codeStore.StoreCode($"verify:{req.Email}", req.Email);
        await _email.SendVerificationEmailAsync(req.Email, code);
        return Ok(new MessageResponse("Verification code sent to your email."));
    }

    // ──────────────── Registration ────────────────

    [HttpPost("register-user")]
    public async Task<ActionResult<AuthResponse>> RegisterUser(RegisterUserRequest req)
    {
        var data = _codeStore.RedeemCode($"verify:{req.Email}", req.Code);
        if (data is null)
            return BadRequest(new { error = "Invalid or expired verification code. Please request a new one." });

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            EmailConfirmed = true,
            AccountType = AccountType.User,
            FullName = req.FullName,
            Country = req.Country
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "User");

        var (token, refreshToken, expiresIn) = await _jwt.CreateTokenPairAsync(user);
        return Ok(new AuthResponse(token, refreshToken, user.AccountType.ToString(), expiresIn));
    }

    [HttpPost("register-shop")]
    public async Task<ActionResult<AuthResponse>> RegisterShop(RegisterShopRequest req)
    {
        var data = _codeStore.RedeemCode($"verify:{req.Email}", req.Code);
        if (data is null)
            return BadRequest(new { error = "Invalid or expired verification code. Please request a new one." });

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            EmailConfirmed = true,
            AccountType = AccountType.Shop,
            FullName = req.ShopName,
            Country = req.Country
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "Shop");

        var profile = new ShopProfile
        {
            UserId = user.Id,
            ShopName = req.ShopName,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ShopProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var (token, refreshToken, expiresIn) = await _jwt.CreateTokenPairAsync(user);
        return Ok(new AuthResponse(token, refreshToken, user.AccountType.ToString(), expiresIn));
    }

    [HttpPost("register-delivery")]
    public async Task<ActionResult<AuthResponse>> RegisterDelivery(RegisterDeliveryRequest req)
    {
        var data = _codeStore.RedeemCode($"verify:{req.Email}", req.Code);
        if (data is null)
            return BadRequest(new { error = "Invalid or expired verification code. Please request a new one." });

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            EmailConfirmed = true,
            AccountType = AccountType.Delivery,
            FullName = req.FullName,
            Country = req.Country
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, "Delivery");

        var profile = new DeliveryProfile
        {
            UserId = user.Id,
            Phone = req.Phone,
            VehicleInfo = req.VehicleInfo,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.DeliveryProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var (token, refreshToken, expiresIn) = await _jwt.CreateTokenPairAsync(user);
        return Ok(new AuthResponse(token, refreshToken, user.AccountType.ToString(), expiresIn));
    }

    // ──────────────── Login ────────────────

    [HttpPost("login")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized(new { error = "Invalid email or password" });

        // Check if user is disabled
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            return Unauthorized(new { error = "Your account has been disabled. Contact support." });

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded) return Unauthorized(new { error = "Invalid email or password" });

        var (token, refreshToken, expiresIn) = await _jwt.CreateTokenPairAsync(user);
        return Ok(new AuthResponse(token, refreshToken, user.AccountType.ToString(), expiresIn));
    }

    // ──────────────── Refresh Token ────────────────

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest req)
    {
        var result = await _jwt.RefreshAsync(req.AccessToken, req.RefreshToken);
        if (result is null)
            return Unauthorized(new { error = "Invalid or expired refresh token" });

        var (accessToken, refreshToken, expiresIn) = result.Value;

        // Get account type from new access token
        var principal = _jwt.GetPrincipalFromExpiredToken(accessToken);
        var accountType = principal?.FindFirst("accountType")?.Value ?? "User";

        return Ok(new AuthResponse(accessToken, refreshToken, accountType, expiresIn));
    }

    // ──────────────── Logout (revoke refresh token) ────────────────

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest req)
    {
        await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);
        return Ok(new MessageResponse("Logged out successfully."));
    }

    // ──────────────── Forgot / Reset Password ────────────────

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            return Ok(new MessageResponse("If an account with that email exists, a reset code has been sent."));
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var code = _codeStore.StoreCode($"reset:{req.Email}", resetToken);

        await _email.SendPasswordResetEmailAsync(req.Email, code);

        return Ok(new MessageResponse("If an account with that email exists, a reset code has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return BadRequest(new { error = "Invalid email address" });

        var resetToken = _codeStore.RedeemCode($"reset:{req.Email}", req.Code);
        if (resetToken is null)
            return BadRequest(new { error = "Invalid or expired reset code" });

        var result = await _userManager.ResetPasswordAsync(user, resetToken, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new MessageResponse("Password has been reset successfully. You can now log in."));
    }
}
