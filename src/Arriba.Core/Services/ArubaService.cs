using Arriba.Core.Models;

namespace Arriba.Core.Services;

public interface IArubaService
{
    Task<ApiResponse<AuthTokens>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthTokens>> RefreshAuthenticationAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<Site>>> GetSitesAsync(AuthTokens tokens, CancellationToken cancellationToken = default);
    Task<ApiResponse<Site>> GetSiteWithDevicesAsync(AuthTokens tokens, string siteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<Device>>> GetDevicesAsync(AuthTokens tokens, string siteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<Device>> GetDeviceWithRadiosAsync(AuthTokens tokens, string siteId, string deviceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<RadioControlResponse>> ToggleRadioAsync(AuthTokens tokens, string siteId, string deviceId, string radioId, bool enabled, CancellationToken cancellationToken = default);
    Task<ApiResponse<RadioControlResponse>> UpdateRadioAsync(AuthTokens tokens, string siteId, RadioControlRequest request, CancellationToken cancellationToken = default);
}

public class ArubaService : IArubaService
{
    private readonly IArubaApiClient _apiClient;

    public ArubaService(IArubaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<AuthTokens>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.LoginAsync(new LoginRequest(email, password), cancellationToken);

        if (!response.Success || response.Data == null)
        {
            return ApiResponse<AuthTokens>.Fail(response.Error ?? "Authentication failed", response.StatusCode);
        }

        var tokens = new AuthTokens
        {
            AccessToken = response.Data.AccessToken,
            RefreshToken = response.Data.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(response.Data.ExpiresIn),
            TokenType = response.Data.TokenType
        };

        return ApiResponse<AuthTokens>.Ok(tokens);
    }

    public async Task<ApiResponse<AuthTokens>> RefreshAuthenticationAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.RefreshTokenAsync(refreshToken, cancellationToken);

        if (!response.Success || response.Data == null)
        {
            return ApiResponse<AuthTokens>.Fail(response.Error ?? "Token refresh failed", response.StatusCode);
        }

        var tokens = new AuthTokens
        {
            AccessToken = response.Data.AccessToken,
            RefreshToken = response.Data.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(response.Data.ExpiresIn),
            TokenType = response.Data.TokenType
        };

        return ApiResponse<AuthTokens>.Ok(tokens);
    }

    public async Task<ApiResponse<List<Site>>> GetSitesAsync(AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetSitesAsync(tokens.AccessToken, cancellationToken);
    }

    public async Task<ApiResponse<Site>> GetSiteWithDevicesAsync(AuthTokens tokens, string siteId, CancellationToken cancellationToken = default)
    {
        var siteResponse = await _apiClient.GetSiteAsync(tokens.AccessToken, siteId, cancellationToken);

        if (!siteResponse.Success || siteResponse.Data == null)
        {
            return siteResponse;
        }

        var devicesResponse = await _apiClient.GetDevicesAsync(tokens.AccessToken, siteId, cancellationToken);

        if (!devicesResponse.Success)
        {
            return siteResponse;
        }

        // Fetch radios for all devices in parallel to avoid N+1 query issue
        var devices = devicesResponse.Data ?? new List<Device>();
        var radioTasks = devices.Select(async device =>
        {
            var radiosResponse = await _apiClient.GetRadiosAsync(tokens.AccessToken, siteId, device.Id, cancellationToken);
            return device with { Radios = radiosResponse.Data ?? new List<Radio>() };
        });

        var devicesWithRadios = await Task.WhenAll(radioTasks);

        var siteWithDevices = siteResponse.Data with { Devices = devicesWithRadios.ToList() };
        return ApiResponse<Site>.Ok(siteWithDevices);
    }

    public async Task<ApiResponse<List<Device>>> GetDevicesAsync(AuthTokens tokens, string siteId, CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetDevicesAsync(tokens.AccessToken, siteId, cancellationToken);
    }

    public async Task<ApiResponse<Device>> GetDeviceWithRadiosAsync(AuthTokens tokens, string siteId, string deviceId, CancellationToken cancellationToken = default)
    {
        var deviceResponse = await _apiClient.GetDeviceAsync(tokens.AccessToken, siteId, deviceId, cancellationToken);

        if (!deviceResponse.Success || deviceResponse.Data == null)
        {
            return deviceResponse;
        }

        var radiosResponse = await _apiClient.GetRadiosAsync(tokens.AccessToken, siteId, deviceId, cancellationToken);

        var deviceWithRadios = deviceResponse.Data with { Radios = radiosResponse.Data ?? new List<Radio>() };
        return ApiResponse<Device>.Ok(deviceWithRadios);
    }

    public async Task<ApiResponse<RadioControlResponse>> ToggleRadioAsync(AuthTokens tokens, string siteId, string deviceId, string radioId, bool enabled, CancellationToken cancellationToken = default)
    {
        var request = new RadioControlRequest(deviceId, radioId, enabled, null, null);
        return await _apiClient.ControlRadioAsync(tokens.AccessToken, siteId, request, cancellationToken);
    }

    public async Task<ApiResponse<RadioControlResponse>> UpdateRadioAsync(AuthTokens tokens, string siteId, RadioControlRequest request, CancellationToken cancellationToken = default)
    {
        return await _apiClient.ControlRadioAsync(tokens.AccessToken, siteId, request, cancellationToken);
    }
}
