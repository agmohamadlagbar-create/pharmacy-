using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly OnlineStore.Api.Services.IFileStorageService _fileStorage;

    public MeController(UserManager<AppUser> userManager, ApplicationDbContext db, OnlineStore.Api.Services.IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _db = db;
        _fileStorage = fileStorage;
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet]
    public async Task<ActionResult<MeResponse>> GetMe()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        // Single query: LEFT JOIN user with their ShopProfile (if Shop account)
        var data = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                User = u,
                Shop = _db.ShopProfiles
                    .Where(s => s.UserId == u.Id)
                    .Select(s => new { s.ShopName, s.Location, s.StoreType, s.Description, s.OpeningTime, s.ClosingTime })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (data?.User is null) return Unauthorized();
        var user = data.User;

        return Ok(new MeResponse(
            user.Id,
            user.Email ?? "",
            user.AccountType.ToString(),
            user.FullName,
            data.Shop?.ShopName,
            data.Shop?.Location,
            data.Shop?.StoreType,
            user.ProfilePictureUrl,
            user.Country,
            user.Street,
            user.BuildingNumber,
            user.City,
            user.PostalCode,
            user.District,
            user.Neighborhood,
            user.Floor,
            user.ApartmentNo,
            user.Latitude,
            user.Longitude,
            data.Shop?.Description,
            data.Shop?.OpeningTime,
            data.Shop?.ClosingTime
        ));
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // Archive old picture instead of hard-deleting
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            // Extract storage key from the /media/ URL
            var oldKey = user.ProfilePictureUrl.Replace("/media/", "");
            _fileStorage.ArchiveFile(oldKey);
        }

        var country = user.Country ?? "Unknown";
        var city = user.City ?? "Unknown";

        try
        {
            var storageKey = await _fileStorage.SaveUserAvatarAsync(country, city, user.Id, file);
            user.ProfilePictureUrl = "/media/" + storageKey;
            await _userManager.UpdateAsync(user);

            return Ok(new { profilePictureUrl = user.ProfilePictureUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        // Validate required address fields for Customer accounts
        if (user.AccountType == OnlineStore.Domain.Entities.AccountType.User)
        {
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Street)) missingFields.Add("Address Information");
            if (string.IsNullOrWhiteSpace(request.District)) missingFields.Add("District");
            if (string.IsNullOrWhiteSpace(request.Neighborhood)) missingFields.Add("Neighborhood");
            if (string.IsNullOrWhiteSpace(request.BuildingNumber)) missingFields.Add("Building Number");
            if (string.IsNullOrWhiteSpace(request.Floor)) missingFields.Add("Floor");
            if (string.IsNullOrWhiteSpace(request.ApartmentNo)) missingFields.Add("Apartment No.");
            if (string.IsNullOrWhiteSpace(request.City)) missingFields.Add("City");

            if (missingFields.Count > 0)
                return BadRequest(new { error = $"The following address fields are required: {string.Join(", ", missingFields)}" });
        }
        // Validate required address fields for Delivery and Shop accounts (Building No, Floor, Apartment No are optional)
        else if (user.AccountType == OnlineStore.Domain.Entities.AccountType.Delivery || user.AccountType == OnlineStore.Domain.Entities.AccountType.Shop)
        {
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Street)) missingFields.Add("Address Information");
            if (string.IsNullOrWhiteSpace(request.District)) missingFields.Add("District");
            if (string.IsNullOrWhiteSpace(request.Neighborhood)) missingFields.Add("Neighborhood");
            if (string.IsNullOrWhiteSpace(request.City)) missingFields.Add("City");

            if (missingFields.Count > 0)
                return BadRequest(new { error = $"The following address fields are required: {string.Join(", ", missingFields)}" });
        }

        user.FullName = request.FullName ?? user.FullName;
        user.Country = request.Country ?? user.Country;
        user.Street = request.Street ?? user.Street;
        user.BuildingNumber = request.BuildingNumber ?? user.BuildingNumber;
        user.City = request.City ?? user.City;
        user.PostalCode = request.PostalCode ?? user.PostalCode;
        user.District = request.District ?? user.District;
        user.Neighborhood = request.Neighborhood ?? user.Neighborhood;
        user.Floor = request.Floor ?? user.Floor;
        user.ApartmentNo = request.ApartmentNo ?? user.ApartmentNo;
        user.Latitude = request.Latitude ?? user.Latitude;
        user.Longitude = request.Longitude ?? user.Longitude;

        if (user.AccountType == OnlineStore.Domain.Entities.AccountType.Shop)
        {
            var shop = await _db.ShopProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (shop != null)
            {
                if (request.FullName != null) shop.ShopName = request.FullName;
                if (request.Description != null) shop.Description = request.Description;
                if (request.OpeningTime != null) shop.OpeningTime = request.OpeningTime;
                if (request.ClosingTime != null) shop.ClosingTime = request.ClosingTime;
            }
        }

        // Sync updated address into UserAddresses table so Checkout stays in sync
        var defaultAddr = await _db.UserAddresses.FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsDefault);
        if (defaultAddr != null)
        {
            defaultAddr.Street = user.Street;
            defaultAddr.BuildingNumber = user.BuildingNumber;
            defaultAddr.City = user.City;
            defaultAddr.PostalCode = user.PostalCode;
            defaultAddr.District = user.District;
            defaultAddr.Neighborhood = user.Neighborhood;
            defaultAddr.Floor = user.Floor;
            defaultAddr.ApartmentNo = user.ApartmentNo;
            defaultAddr.Latitude = user.Latitude;
            defaultAddr.Longitude = user.Longitude;
        }
        else if (!string.IsNullOrWhiteSpace(user.Street))
        {
            _db.UserAddresses.Add(new OnlineStore.Domain.Entities.UserAddress
            {
                UserId = user.Id,
                Title = "Home",
                Street = user.Street,
                BuildingNumber = user.BuildingNumber,
                City = user.City,
                PostalCode = user.PostalCode,
                District = user.District,
                Neighborhood = user.Neighborhood,
                Floor = user.Floor,
                ApartmentNo = user.ApartmentNo,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                IsDefault = true
            });
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully" });
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("addresses")]
    public async Task<ActionResult<List<OnlineStore.Domain.Entities.UserAddress>>> GetAddresses()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var addresses = await _db.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync();

        // If no saved delivery addresses but user has address on profile, auto-create one
        if (addresses.Count == 0)
        {
            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && !string.IsNullOrWhiteSpace(user.Street))
            {
                var profileAddress = new OnlineStore.Domain.Entities.UserAddress
                {
                    UserId = userId,
                    Title = "Home",
                    Street = user.Street,
                    BuildingNumber = user.BuildingNumber,
                    City = user.City,
                    PostalCode = user.PostalCode,
                    District = user.District,
                    Neighborhood = user.Neighborhood,
                    Floor = user.Floor,
                    ApartmentNo = user.ApartmentNo,
                    Latitude = user.Latitude,
                    Longitude = user.Longitude,
                    IsDefault = true
                };
                _db.UserAddresses.Add(profileAddress);
                await _db.SaveChangesAsync();
                addresses.Add(profileAddress);
            }
        }

        return Ok(addresses);
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] SaveAddressRequest req)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var isFirst = !await _db.UserAddresses.AnyAsync(a => a.UserId == userId);

        var address = new OnlineStore.Domain.Entities.UserAddress
        {
            UserId = userId,
            Title = req.Title ?? "Other",
            Street = req.Street,
            BuildingNumber = req.BuildingNumber,
            City = req.City,
            PostalCode = req.PostalCode,
            District = req.District,
            Neighborhood = req.Neighborhood,
            Floor = req.Floor,
            ApartmentNo = req.ApartmentNo,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            IsDefault = isFirst || req.IsDefault
        };

        if (address.IsDefault && !isFirst)
        {
            var defaults = await _db.UserAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
            foreach (var def in defaults) def.IsDefault = false;
        }

        _db.UserAddresses.Add(address);
        await _db.SaveChangesAsync();

        return Ok(address);
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("addresses/{id:int}")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] SaveAddressRequest req)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address is null) return NotFound();

        address.Title = req.Title ?? address.Title;
        address.Street = req.Street ?? address.Street;
        address.BuildingNumber = req.BuildingNumber ?? address.BuildingNumber;
        address.City = req.City ?? address.City;
        address.PostalCode = req.PostalCode ?? address.PostalCode;
        address.District = req.District ?? address.District;
        address.Neighborhood = req.Neighborhood ?? address.Neighborhood;
        address.Floor = req.Floor ?? address.Floor;
        address.ApartmentNo = req.ApartmentNo ?? address.ApartmentNo;
        address.Latitude = req.Latitude ?? address.Latitude;
        address.Longitude = req.Longitude ?? address.Longitude;

        if (req.IsDefault && !address.IsDefault)
        {
            var defaults = await _db.UserAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
            foreach (var def in defaults) def.IsDefault = false;
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync();
        return Ok(address);
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("addresses/{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address is null) return NotFound();

        _db.UserAddresses.Remove(address);
        
        // If it was default, make another one default
        if (address.IsDefault)
        {
            var nextAddress = await _db.UserAddresses.FirstOrDefaultAsync(a => a.UserId == userId && a.Id != id);
            if (nextAddress != null)
            {
                nextAddress.IsDefault = true;
            }
        }
        
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
