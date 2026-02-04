using Arriba.Core.Models;
using Arriba.Core.Services;
using Arriba.Web.Helpers;
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
        var tokens = AuthTokenHelper.GetTokensFromHeader(Request);
        if (tokens == null)
        {
            _logger.LogWarning("GetRadios: Unauthorized - No access token provided for device {DeviceId} on site {SiteId}", deviceId, siteId);
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Fetching radios for device {DeviceId} on site {SiteId}", deviceId, siteId);
        var result = await _arubaService.GetDeviceWithRadiosAsync(tokens, siteId, deviceId, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            _logger.LogError("Failed to fetch radios for device {DeviceId} on site {SiteId}: {Error}", deviceId, siteId, result.Error);
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch radios"));
        }

        _logger.LogInformation("Successfully fetched {Count} radios for device {DeviceId}", result.Data.Radios?.Count ?? 0, deviceId);
        return Ok(result.Data.Radios);
    }

    [HttpPost("{radioId}/toggle")]
    public async Task<IActionResult> ToggleRadio(string siteId, string deviceId, string radioId, [FromBody] ToggleRadioRequest request, CancellationToken cancellationToken)
    {
        var tokens = AuthTokenHelper.GetTokensFromHeader(Request);
        if (tokens == null)
        {
            _logger.LogWarning("ToggleRadio: Unauthorized - No access token provided");
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Toggling radio {RadioId} on device {DeviceId} to {Enabled}", radioId, deviceId, request.Enabled);

        var result = await _arubaService.ToggleRadioAsync(tokens, siteId, deviceId, radioId, request.Enabled, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Failed to toggle radio {RadioId} on device {DeviceId}: {Error}", radioId, deviceId, result.Error);
            return StatusCode(result.StatusCode, new ApiError("TOGGLE_FAILED", result.Error ?? "Failed to toggle radio"));
        }

        _logger.LogInformation("Successfully toggled radio {RadioId} to {Enabled}", radioId, request.Enabled);
        return Ok(result.Data);
    }

    [HttpPatch("{radioId}")]
    public async Task<IActionResult> UpdateRadio(string siteId, string deviceId, string radioId, [FromBody] UpdateRadioRequest request, CancellationToken cancellationToken)
    {
        var tokens = AuthTokenHelper.GetTokensFromHeader(Request);
        if (tokens == null)
        {
            _logger.LogWarning("UpdateRadio: Unauthorized - No access token provided");
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Updating radio {RadioId} on device {DeviceId} (Enabled: {Enabled}, Channel: {Channel}, Power: {Power})", 
            radioId, deviceId, request.Enabled, request.Channel, request.TransmitPower);

        var controlRequest = new RadioControlRequest(deviceId, radioId, request.Enabled, request.Channel, request.TransmitPower);
        var result = await _arubaService.UpdateRadioAsync(tokens, siteId, controlRequest, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Failed to update radio {RadioId} on device {DeviceId}: {Error}", radioId, deviceId, result.Error);
            return StatusCode(result.StatusCode, new ApiError("UPDATE_FAILED", result.Error ?? "Failed to update radio"));
        }

        _logger.LogInformation("Successfully updated radio {RadioId}", radioId);
        return Ok(result.Data);
    }
}

public record ToggleRadioRequest(bool Enabled);

public record UpdateRadioRequest(bool? Enabled, int? Channel, int? TransmitPower);
