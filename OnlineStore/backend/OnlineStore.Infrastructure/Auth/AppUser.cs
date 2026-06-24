using Microsoft.AspNetCore.Identity;
using OnlineStore.Domain.Entities;

namespace OnlineStore.Infrastructure.Auth;

public class AppUser : IdentityUser
{
    public AccountType AccountType { get; set; }
    public string? FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Country { get; set; }

    // Address fields
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? StripeCustomerId { get; set; }
}
