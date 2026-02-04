using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arriba.Web.Controllers;

[ApiController]
[Route("api/sites/{siteId}/devices/{deviceId}/[controller]")]
public class RadiosController : ControllerBase
{
    private readonly IArubaService _arubaService;
    private readonly ILogger<RadiosController> _logger;

    public RadiosController(IArubaService arubaService, ILogger<RadiosController> logger)
    {
        _arubaService = arubaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetRadios(string siteId, string deviceId, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        var result = await _arubaService.GetDeviceWithRadiosAsync(tokens, siteId, deviceId, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch radios"));
        }

        return Ok(result.Data.Radios);
    }

    [HttpPost("{radioId}/toggle")]
    public async Task<IActionResult> ToggleRadio(string siteId, string deviceId, string radioId, [FromBody] ToggleRadioRequest request, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Toggling radio {RadioId} on device {DeviceId} to {Enabled}", radioId, deviceId, request.Enabled);

        var result = await _arubaService.ToggleRadioAsync(tokens, siteId, deviceId, radioId, request.Enabled, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiError("TOGGLE_FAILED", result.Error ?? "Failed to toggle radio"));
        }

        return Ok(result.Data);
    }

    [HttpPatch("{radioId}")]
    public async Task<IActionResult> UpdateRadio(string siteId, string deviceId, string radioId, [FromBody] UpdateRadioRequest request, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Updating radio {RadioId} on device {DeviceId}", radioId, deviceId);

        var controlRequest = new RadioControlRequest(deviceId, radioId, request.Enabled, request.Channel, request.TransmitPower);
        var result = await _arubaService.UpdateRadioAsync(tokens, siteId, controlRequest, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiError("UPDATE_FAILED", result.Error ?? "Failed to update radio"));
        }

        return Ok(result.Data);
    }

    private AuthTokens? GetTokensFromHeader()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
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
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }
}

public record ToggleRadioRequest(bool Enabled);

public record UpdateRadioRequest(bool? Enabled, int? Channel, int? TransmitPower);
