namespace Arriba.Core.Models;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType
);

public record AuthTokens
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}

public record UserInfo(
    string Id,
    string Email,
    string Name
);
