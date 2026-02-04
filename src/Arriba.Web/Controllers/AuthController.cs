using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arriba.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IArubaService _arubaService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IArubaService arubaService, ILogger<AuthController> logger)
    {
        _arubaService = arubaService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiError("INVALID_REQUEST", "Email and password are required"));
        }

        _logger.LogInformation("Login attempt for user: {Email}", request.Email);

        var result = await _arubaService.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            _logger.LogWarning("Login failed for user: {Email}", request.Email);
            return Unauthorized(new ApiError("AUTH_FAILED", result.Error ?? "Authentication failed"));
        }

        _logger.LogInformation("Login successful for user: {Email}", request.Email);

        var response = new
        {
            accessToken = result.Data.AccessToken,
            refreshToken = result.Data.RefreshToken,
            expiresAt = result.Data.ExpiresAt,
            tokenType = result.Data.TokenType
        };

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ApiError("INVALID_REQUEST", "Refresh token is required"));
        }

        var result = await _arubaService.RefreshAuthenticationAsync(request.RefreshToken, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            return Unauthorized(new ApiError("REFRESH_FAILED", result.Error ?? "Token refresh failed"));
        }

        var response = new
        {
            accessToken = result.Data.AccessToken,
            refreshToken = result.Data.RefreshToken,
            expiresAt = result.Data.ExpiresAt,
            tokenType = result.Data.TokenType
        };

        return Ok(response);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }
}

public record RefreshTokenRequest(string RefreshToken);
