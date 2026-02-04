using Arriba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Arriba.Core.Services;

/// <summary>
/// Mock implementation of IArubaApiClient for development and testing.
/// Accepts fake credentials and returns mock data without making real API calls.
/// </summary>
public class MockArubaApiClient : IArubaApiClient
{
    private readonly ILogger<MockArubaApiClient> _logger;
    
    // Fake credentials for testing
    private const string FakeEmail = "test@example.com";
    private const string FakePassword = "password";
    private const string FakeAccessToken = "mock-access-token-12345";
    private const string FakeRefreshToken = "mock-refresh-token-67890";

    public MockArubaApiClient(ILogger<MockArubaApiClient> logger)
    {
        _logger = logger;
    }

    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock login attempt for: {Email}", request.Email);

        // Simulate a small delay to mimic real API
        Task.Delay(500, cancellationToken).Wait(cancellationToken);

        // Accept fake credentials
        if (request.Email == FakeEmail && request.Password == FakePassword)
        {
            _logger.LogInformation("Mock login successful");
            
            var response = new LoginResponse(
                AccessToken: FakeAccessToken,
                RefreshToken: FakeRefreshToken,
                ExpiresIn: 3600,
                TokenType: "Bearer"
            );

            return Task.FromResult(ApiResponse<LoginResponse>.Ok(response));
        }

        _logger.LogWarning("Mock login failed - invalid credentials");
        return Task.FromResult(ApiResponse<LoginResponse>.Fail("Invalid credentials", 401));
    }

    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock token refresh");

        // Simulate a small delay
        Task.Delay(300, cancellationToken).Wait(cancellationToken);

        if (refreshToken == FakeRefreshToken)
        {
            _logger.LogInformation("Mock token refresh successful");
            
            var response = new LoginResponse(
                AccessToken: $"{FakeAccessToken}-refreshed",
                RefreshToken: FakeRefreshToken,
                ExpiresIn: 3600,
                TokenType: "Bearer"
            );

            return Task.FromResult(ApiResponse<LoginResponse>.Ok(response));
        }

        _logger.LogWarning("Mock token refresh failed");
        return Task.FromResult(ApiResponse<LoginResponse>.Fail("Invalid refresh token", 401));
    }

    public Task<ApiResponse<UserInfo>> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock get user info");
        
        if (accessToken.StartsWith(FakeAccessToken))
        {
            var userInfo = new UserInfo("mock-user-id", FakeEmail, "Mock User");
            return Task.FromResult(ApiResponse<UserInfo>.Ok(userInfo));
        }

        return Task.FromResult(ApiResponse<UserInfo>.Fail("Invalid token", 401));
    }

    public Task<ApiResponse<List<Site>>> GetSitesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock get sites");
        
        if (!accessToken.StartsWith(FakeAccessToken))
        {
            return Task.FromResult(ApiResponse<List<Site>>.Fail("Invalid token", 401));
        }

        var sites = new List<Site>
        {
            new Site(
                Id: "mock-site-1",
                Name: "Mock Site 1",
                Description: "Test site for development",
                TimeZone: "UTC",
                Devices: new List<Device>
                {
                    new Device(
                        Id: "mock-device-1",
                        Name: "Mock AP 1",
                        MacAddress: "00:11:22:33:44:55",
                        Model: "AP22",
                        SerialNumber: "MOCK12345",
                        Status: DeviceStatus.Online,
                        Radios: new List<Radio>
                        {
                            new Radio(
                                Id: "mock-radio-1",
                                Band: "2.4GHz",
                                Channel: 6,
                                ChannelWidth: 20,
                                TransmitPower: 17,
                                Enabled: true,
                                Status: RadioStatus.Active
                            ),
                            new Radio(
                                Id: "mock-radio-2",
                                Band: "5GHz",
                                Channel: 36,
                                ChannelWidth: 40,
                                TransmitPower: 23,
                                Enabled: true,
                                Status: RadioStatus.Active
                            )
                        }
                    )
                }
            )
        };

        return Task.FromResult(ApiResponse<List<Site>>.Ok(sites));
    }

    public Task<ApiResponse<Site>> GetSiteAsync(string accessToken, string siteId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock get site: {SiteId}", siteId);
        
        if (!accessToken.StartsWith(FakeAccessToken))
        {
            return Task.FromResult(ApiResponse<Site>.Fail("Invalid token", 401));
        }

        var site = new Site(
            Id: siteId,
            Name: $"Mock Site {siteId}",
            Description: "Test site for development",
            TimeZone: "UTC",
            Devices: new List<Device>()
        );

        return Task.FromResult(ApiResponse<Site>.Ok(site));
    }

    public Task<ApiResponse<List<Device>>> GetDevicesAsync(string accessToken, string siteId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock get devices for site: {SiteId}", siteId);
        
        if (!accessToken.StartsWith(FakeAccessToken))
        {
            return Task.FromResult(ApiResponse<List<Device>>.Fail("Invalid token", 401));
        }

        var devices = new List<Device>
        {
            new Device(
                Id: "mock-device-1",
                Name: "Mock AP 1",
                MacAddress: "00:11:22:33:44:55",
                Model: "AP22",
                SerialNumber: "MOCK12345",
                Status: DeviceStatus.Online,
                Radios: new List<Radio>()
            )
        };

        return Task.FromResult(ApiResponse<List<Device>>.Ok(devices));
    }

    public Task<ApiResponse<Device>> GetDeviceAsync(string accessToken, string siteId, string deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock get device: {DeviceId}", deviceId);
        
        if (!accessToken.StartsWith(FakeAccessToken))
        {
            return Task.FromResult(ApiResponse<Device>.Fail("Invalid token", 401));
        }

        var device = new Device(
            Id: deviceId,
            Name: $"Mock AP {deviceId}",
            MacAddress: "00:11:22:33:44:55",
            Model: "AP22",
            SerialNumber: "MOCK12345",
            Status: DeviceStatus.Online,
            Radios: new List<Radio>()
        );

        return Task.FromResult(ApiResponse<Device>.Ok(device));
    }

    public Task<ApiResponse<List<Radio>>> GetRadiosAsync(string accessToken, string siteId, string deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mock get radios for device: {DeviceId}", deviceId);
        
        if (!accessToken.StartsWith(FakeAccessToken))
        {
            return Task.FromResult(ApiResponse<List<Radio>>.Fail("Invalid token", 401));
        }

        var radios = new List<Radio>
        {
            new Radio(
                Id: "mock-radio-1",
                Band: "2.4GHz",
                Channel: 6,
                ChannelWidth: 20,
                TransmitPower: 17,
                Enabled: true,
                Status: RadioStatus.Active
            ),
            new Radio(
                Id: "mock-radio-2",
                Band: "5GHz",
                Channel: 36,
                ChannelWidth: 40,
                TransmitPower: 23,
                Enabled: true,
                Status: RadioStatus.Active
            )
        };

        return Task.FromResult(ApiResponse<List<Radio>>.Ok(radios));
    }

    public Task<ApiResponse<RadioControlResponse>> ControlRadioAsync(string accessToken, string siteId, RadioControlRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock control radio: {RadioId}, Enabled: {Enabled}", request.RadioId, request.Enabled);
        
        if (!accessToken.StartsWith(FakeAccessToken))
        {
            return Task.FromResult(ApiResponse<RadioControlResponse>.Fail("Invalid token", 401));
        }

        // Simulate delay for radio control
        Task.Delay(1000, cancellationToken).Wait(cancellationToken);

        var updatedRadio = new Radio(
            Id: request.RadioId,
            Band: "2.4GHz",
            Channel: request.Channel ?? 6,
            ChannelWidth: 20,
            TransmitPower: request.TransmitPower ?? 17,
            Enabled: request.Enabled ?? true,
            Status: request.Enabled == true ? RadioStatus.Active : RadioStatus.Inactive
        );

        var response = new RadioControlResponse(
            Success: true,
            Message: "Radio updated successfully",
            Radio: updatedRadio
        );

        return Task.FromResult(ApiResponse<RadioControlResponse>.Ok(response));
    }
}
