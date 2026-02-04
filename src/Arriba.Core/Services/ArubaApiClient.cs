using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arriba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Arriba.Core.Services;

public class ArubaApiClient : IArubaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ArubaApiClient> _logger;

    private const string BaseUrl = "https://nb.portal.arubainstanton.com/api";
    private const string AuthUrl = "https://sso.arubainstanton.com";

    public ArubaApiClient(HttpClient httpClient, ILogger<ArubaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Email}", request.Email);

            var authRequest = new
            {
                username = request.Email,
                password = request.Password
            };

            _logger.LogDebug("Sending login request to Aruba SSO: {Url}", $"{AuthUrl}/aio/api/v1/mfa/validate/full");

            var response = await _httpClient.PostAsJsonAsync(
                $"{AuthUrl}/aio/api/v1/mfa/validate/full",
                authRequest,
                _jsonOptions,
                cancellationToken);

            _logger.LogDebug("Received response from Aruba SSO: Status {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Login failed for user {Email}: Status {StatusCode}, Error: {Error}", 
                    request.Email, response.StatusCode, errorContent);
                return ApiResponse<LoginResponse>.Fail($"Authentication failed: {errorContent}", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaAuthResponse>(_jsonOptions, cancellationToken);

            if (result?.AccessToken == null)
            {
                _logger.LogError("Login failed for user {Email}: Invalid authentication response", request.Email);
                return ApiResponse<LoginResponse>.Fail("Invalid authentication response", 500);
            }

            _logger.LogInformation("Login successful for user: {Email}", request.Email);

            return ApiResponse<LoginResponse>.Ok(new LoginResponse(
                result.AccessToken,
                result.RefreshToken ?? string.Empty,
                result.ExpiresIn,
                result.TokenType ?? "Bearer"
            ));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Login request timed out for user {Email}", request.Email);
            return ApiResponse<LoginResponse>.Fail("Login request timed out. Please try again.", 408);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Login network error for user {Email}: {Message}", request.Email, ex.Message);
            return ApiResponse<LoginResponse>.Fail($"Network error: {ex.Message}", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login exception for user {Email}: {Message}", request.Email, ex.Message);
            return ApiResponse<LoginResponse>.Fail($"Login error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to refresh authentication token");

            var refreshRequest = new { refresh_token = refreshToken };

            _logger.LogDebug("Sending token refresh request to Aruba SSO");

            var response = await _httpClient.PostAsJsonAsync(
                $"{AuthUrl}/aio/api/v1/refresh",
                refreshRequest,
                _jsonOptions,
                cancellationToken);

            _logger.LogDebug("Received token refresh response: Status {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed: Status {StatusCode}", response.StatusCode);
                return ApiResponse<LoginResponse>.Fail("Token refresh failed", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaAuthResponse>(_jsonOptions, cancellationToken);

            if (result?.AccessToken == null)
            {
                _logger.LogError("Token refresh failed: Invalid refresh response");
                return ApiResponse<LoginResponse>.Fail("Invalid refresh response", 500);
            }

            _logger.LogInformation("Token refresh successful");

            return ApiResponse<LoginResponse>.Ok(new LoginResponse(
                result.AccessToken,
                result.RefreshToken ?? refreshToken,
                result.ExpiresIn,
                result.TokenType ?? "Bearer"
            ));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Token refresh request timed out");
            return ApiResponse<LoginResponse>.Fail("Token refresh timed out. Please try again.", 408);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Token refresh network error: {Message}", ex.Message);
            return ApiResponse<LoginResponse>.Fail($"Network error: {ex.Message}", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh exception: {Message}", ex.Message);
            return ApiResponse<LoginResponse>.Fail($"Refresh error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<UserInfo>> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/userinfo", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<UserInfo>.Fail("Failed to get user info", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaUserInfo>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                return ApiResponse<UserInfo>.Fail("Invalid user info response", 500);
            }

            return ApiResponse<UserInfo>.Ok(new UserInfo(result.Id, result.Email, result.Name ?? result.Email));
        }
        catch (Exception ex)
        {
            return ApiResponse<UserInfo>.Fail($"User info error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<List<Site>>> GetSitesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching sites from Aruba API");

            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/sites", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get sites from Aruba API: Status {StatusCode}", response.StatusCode);
                return ApiResponse<List<Site>>.Fail("Failed to get sites", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaSitesResponse>(_jsonOptions, cancellationToken);
            var sites = result?.Elements?.Select(MapToSite).ToList() ?? new List<Site>();

            _logger.LogDebug("Successfully fetched {Count} sites from Aruba API", sites.Count);

            return ApiResponse<List<Site>>.Ok(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching sites: {Message}", ex.Message);
            return ApiResponse<List<Site>>.Fail($"Sites error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<Site>> GetSiteAsync(string accessToken, string siteId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/sites/{siteId}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<Site>.Fail("Failed to get site", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaSiteElement>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                return ApiResponse<Site>.Fail("Site not found", 404);
            }

            return ApiResponse<Site>.Ok(MapToSite(result));
        }
        catch (Exception ex)
        {
            return ApiResponse<Site>.Fail($"Site error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<List<Device>>> GetDevicesAsync(string accessToken, string siteId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching devices for site {SiteId} from Aruba API", siteId);

            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/sites/{siteId}/devices", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get devices for site {SiteId} from Aruba API: Status {StatusCode}", siteId, response.StatusCode);
                return ApiResponse<List<Device>>.Fail("Failed to get devices", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaDevicesResponse>(_jsonOptions, cancellationToken);
            var devices = result?.Elements?.Select(MapToDevice).ToList() ?? new List<Device>();

            _logger.LogDebug("Successfully fetched {Count} devices for site {SiteId} from Aruba API", devices.Count, siteId);

            return ApiResponse<List<Device>>.Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching devices for site {SiteId}: {Message}", siteId, ex.Message);
            return ApiResponse<List<Device>>.Fail($"Devices error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<Device>> GetDeviceAsync(string accessToken, string siteId, string deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/sites/{siteId}/devices/{deviceId}", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<Device>.Fail("Failed to get device", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaDeviceElement>(_jsonOptions, cancellationToken);

            if (result == null)
            {
                return ApiResponse<Device>.Fail("Device not found", 404);
            }

            return ApiResponse<Device>.Ok(MapToDevice(result));
        }
        catch (Exception ex)
        {
            return ApiResponse<Device>.Fail($"Device error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<List<Radio>>> GetRadiosAsync(string accessToken, string siteId, string deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/sites/{siteId}/devices/{deviceId}/radios", accessToken);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<List<Radio>>.Fail("Failed to get radios", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaRadiosResponse>(_jsonOptions, cancellationToken);
            var radios = result?.Elements?.Select(MapToRadio).ToList() ?? new List<Radio>();

            return ApiResponse<List<Radio>>.Ok(radios);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<Radio>>.Fail($"Radios error: {ex.Message}", 500);
        }
    }

    public async Task<ApiResponse<RadioControlResponse>> ControlRadioAsync(string accessToken, string siteId, RadioControlRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateRequest = new ArubaRadioUpdateRequest
            {
                Enabled = request.Enabled,
                Channel = request.Channel,
                TransmitPower = request.TransmitPower
            };

            using var httpRequest = CreateRequest(
                HttpMethod.Patch,
                $"{BaseUrl}/sites/{siteId}/devices/{request.DeviceId}/radios/{request.RadioId}",
                accessToken);

            httpRequest.Content = JsonContent.Create(updateRequest, options: _jsonOptions);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return ApiResponse<RadioControlResponse>.Fail($"Failed to control radio: {errorContent}", (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<ArubaRadioElement>(_jsonOptions, cancellationToken);
            var radio = result != null ? MapToRadio(result) : null;

            return ApiResponse<RadioControlResponse>.Ok(new RadioControlResponse(true, "Radio updated successfully", radio));
        }
        catch (Exception ex)
        {
            return ApiResponse<RadioControlResponse>.Fail($"Radio control error: {ex.Message}", 500);
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static Site MapToSite(ArubaSiteElement element) => new(
        element.Id,
        element.Name,
        element.Description,
        element.TimeZone,
        element.Devices?.Select(MapToDevice).ToList() ?? new List<Device>()
    );

    private static Device MapToDevice(ArubaDeviceElement element) => new(
        element.Id,
        element.Name,
        element.MacAddress,
        element.Model ?? "Unknown",
        element.SerialNumber ?? "Unknown",
        MapDeviceStatus(element.Status),
        element.Radios?.Select(MapToRadio).ToList() ?? new List<Radio>()
    );

    private static Radio MapToRadio(ArubaRadioElement element) => new(
        element.Id,
        element.Band ?? "Unknown",
        element.Channel,
        element.ChannelWidth,
        element.TransmitPower,
        element.Enabled,
        MapRadioStatus(element.Status)
    );

    private static DeviceStatus MapDeviceStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "online" or "up" => DeviceStatus.Online,
        "offline" or "down" => DeviceStatus.Offline,
        "updating" => DeviceStatus.Updating,
        _ => DeviceStatus.Unknown
    };

    private static RadioStatus MapRadioStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" or "up" => RadioStatus.Active,
        "inactive" or "down" => RadioStatus.Inactive,
        "disabled" => RadioStatus.Disabled,
        _ => RadioStatus.Unknown
    };
}

// Internal DTOs for Aruba API responses
internal record ArubaAuthResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string? TokenType
);

internal record ArubaUserInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name")] string? Name
);

internal record ArubaSitesResponse(
    [property: JsonPropertyName("elements")] List<ArubaSiteElement>? Elements
);

internal record ArubaSiteElement(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("timezone")] string? TimeZone,
    [property: JsonPropertyName("devices")] List<ArubaDeviceElement>? Devices
);

internal record ArubaDevicesResponse(
    [property: JsonPropertyName("elements")] List<ArubaDeviceElement>? Elements
);

internal record ArubaDeviceElement(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("macAddress")] string MacAddress,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("serialNumber")] string? SerialNumber,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("radios")] List<ArubaRadioElement>? Radios
);

internal record ArubaRadiosResponse(
    [property: JsonPropertyName("elements")] List<ArubaRadioElement>? Elements
);

internal record ArubaRadioElement(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("band")] string? Band,
    [property: JsonPropertyName("channel")] int Channel,
    [property: JsonPropertyName("channelWidth")] int ChannelWidth,
    [property: JsonPropertyName("transmitPower")] int TransmitPower,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("status")] string? Status
);

internal record ArubaRadioUpdateRequest
{
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; init; }

    [JsonPropertyName("channel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Channel { get; init; }

    [JsonPropertyName("transmitPower")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TransmitPower { get; init; }
}
