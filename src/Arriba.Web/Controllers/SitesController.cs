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
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        var result = await _arubaService.GetSitesAsync(tokens, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch sites"));
        }

        return Ok(result.Data);
    }

    [HttpGet("{siteId}")]
    public async Task<IActionResult> GetSite(string siteId, [FromQuery] bool includeDevices = true, CancellationToken cancellationToken = default)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        var result = includeDevices
            ? await _arubaService.GetSiteWithDevicesAsync(tokens, siteId, cancellationToken)
            : await _arubaService.GetSitesAsync(tokens, cancellationToken).ContinueWith(t =>
                t.Result.Success && t.Result.Data != null
                    ? ApiResponse<Site>.Ok(t.Result.Data.FirstOrDefault(s => s.Id == siteId)!)
                    : ApiResponse<Site>.Fail(t.Result.Error ?? "Site not found", t.Result.StatusCode), cancellationToken);

        if (!result.Success || result.Data == null)
        {
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch site"));
        }

        return Ok(result.Data);
    }

    [HttpGet("{siteId}/devices")]
    public async Task<IActionResult> GetDevices(string siteId, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        var result = await _arubaService.GetDevicesAsync(tokens, siteId, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch devices"));
        }

        return Ok(result.Data);
    }

    [HttpGet("{siteId}/devices/{deviceId}")]
    public async Task<IActionResult> GetDevice(string siteId, string deviceId, CancellationToken cancellationToken)
    {
        var tokens = GetTokensFromHeader();
        if (tokens == null)
        {
            return Unauthorized(new ApiError("UNAUTHORIZED", "Access token required"));
        }

        var result = await _arubaService.GetDeviceWithRadiosAsync(tokens, siteId, deviceId, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            return StatusCode(result.StatusCode, new ApiError("FETCH_FAILED", result.Error ?? "Failed to fetch device"));
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
