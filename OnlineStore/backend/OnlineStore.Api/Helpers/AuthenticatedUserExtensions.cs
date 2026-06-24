using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OnlineStore.Api.Helpers;

public static class AuthenticatedUserExtensions
{
    public static string? GetAuthenticatedUserId(this ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst("sub")?.Value;
    }
}
