using Arriba.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Arriba.Web.Helpers;

public static class AuthTokenHelper
{
    /// <summary>
    /// Extracts the access token from the Authorization header and returns an AuthTokens object.
    /// Returns null if the header is missing or invalid.
    /// </summary>
    public static AuthTokens? GetTokensFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var accessToken = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return new AuthTokens
        {
            AccessToken = accessToken,
            RefreshToken = string.Empty,
            // Token expiry should be managed by the client or extracted from the JWT
            // Setting a far future date since we don't have the actual expiry here
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };
    }
}
