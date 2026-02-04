using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Arriba.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    private readonly IArubaService _arubaService;
    private readonly ILogger<SitesController> _logger;

    public SitesController(IArubaService arubaService, ILogger<SitesController> logger)
    {
        _arubaService = arubaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSites(CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            _logger.LogWarning("GetSites: Unauthorized - No access token provided");
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Fetching all sites");
        var result = await _arubaService.GetSitesAsync(tokens, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Failed to fetch sites: {Error}", result.Error);
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch sites"));
        }

        _logger.LogInformation("Successfully fetched {Count} sites", result.Data?.Count ?? 0);
        return Ok(result.Data);
    }

    [HttpGet("{siteId}")]
    public async Task<IActionResult> GetSite(string siteId, [FromQuery] bool includeDevices = true, CancellationToken cancellationToken = default)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            _logger.LogWarning("GetSite: Unauthorized - No access token provided for site {SiteId}", siteId);
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Fetching site {SiteId} (includeDevices: {IncludeDevices})", siteId, includeDevices);

        var result = includeDevices
            ? await _arubaService.GetSiteWithDevicesAsync(tokens, siteId, cancellationToken)
            : await _arubaService.GetSitesAsync(tokens, cancellationToken).ContinueWith(t =>
                t.Result.Success && t.Result.Data != null
                    ? ApiResponse<Site>.Ok(t.Result.Data.FirstOrDefault(s => s.Id == siteId)!)
                    : ApiResponse<Site>.Fail(t.Result.Error ?? "Site not found", t.Result.StatusCode), cancellationToken);

        if (!result.Success || result.Data == null)
        {
            _logger.LogError("Failed to fetch site {SiteId}: {Error}", siteId, result.Error);
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch site"));
        }

        _logger.LogInformation("Successfully fetched site {SiteId}", siteId);
        return Ok(result.Data);
    }

    [HttpGet("{siteId}/devices")]
    public async Task<IActionResult> GetDevices(string siteId, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            _logger.LogWarning("GetDevices: Unauthorized - No access token provided for site {SiteId}", siteId);
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Fetching devices for site {SiteId}", siteId);
        var result = await _arubaService.GetDevicesAsync(tokens, siteId, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Failed to fetch devices for site {SiteId}: {Error}", siteId, result.Error);
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch devices"));
        }

        _logger.LogInformation("Successfully fetched {Count} devices for site {SiteId}", result.Data?.Count ?? 0, siteId);
        return Ok(result.Data);
    }

    [HttpGet("{siteId}/devices/{deviceId}")]
    public async Task<IActionResult> GetDevice(string siteId, string deviceId, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            _logger.LogWarning("GetDevice: Unauthorized - No access token provided for device {DeviceId} on site {SiteId}", deviceId, siteId);
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        _logger.LogInformation("Fetching device {DeviceId} on site {SiteId}", deviceId, siteId);
        var result = await _arubaService.GetDeviceWithRadiosAsync(tokens, siteId, deviceId, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            _logger.LogError("Failed to fetch device {DeviceId} on site {SiteId}: {Error}", deviceId, siteId, result.Error);
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch device"));
        }

        _logger.LogInformation("Successfully fetched device {DeviceId} with {RadioCount} radios", deviceId, result.Data.Radios?.Count ?? 0);
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
            // Token expiry should be managed by the client or extracted from the JWT
            // Setting a far future date since we don't have the actual expiry here
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };
    }
}
