using Arriba.Core.Models;
using Arriba.Core.Services;
using Moq;

namespace Arriba.Tests.Services;

public class ArubaServiceTests
{
    private readonly Mock<IArubaApiClient> _apiClientMock;
    private readonly ArubaService _service;

    public ArubaServiceTests()
    {
        _apiClientMock = new Mock<IArubaApiClient>();
        _service = new ArubaService(_apiClientMock.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var loginResponse = new LoginResponse(
            "access-token",
            "refresh-token",
            3600,
            "Bearer"
        );

        _apiClientMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<LoginResponse>.Ok(loginResponse));

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "password");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("access-token", result.Data.AccessToken);
        Assert.Equal("refresh-token", result.Data.RefreshToken);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<LoginResponse>.Fail("Invalid credentials", 401));

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "wrong-password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task GetSitesAsync_ReturnsSites()
    {
        // Arrange
        var sites = new List<Site>
        {
            new("site-1", "Test Site", null, null, new List<Device>())
        };

        _apiClientMock
            .Setup(x => x.GetSitesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<List<Site>>.Ok(sites));

        var tokens = CreateTestTokens();

        // Act
        var result = await _service.GetSitesAsync(tokens);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetSiteWithDevicesAsync_ReturnsSiteWithDevices()
    {
        // Arrange
        var site = new Site("site-1", "Test Site", null, null, new List<Device>());
        var devices = new List<Device>
        {
            new("device-1", "AP-1", "20:9c:b4:c5:dd:be", "AP11D", "SN123", DeviceStatus.Online, new List<Radio>())
        };
        var radios = new List<Radio>
        {
            new("radio-1", "2.4GHz", 6, 20, 17, true, RadioStatus.Active)
        };

        _apiClientMock
            .Setup(x => x.GetSiteAsync(It.IsAny<string>(), "site-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Site>.Ok(site));

        _apiClientMock
            .Setup(x => x.GetDevicesAsync(It.IsAny<string>(), "site-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<List<Device>>.Ok(devices));

        _apiClientMock
            .Setup(x => x.GetRadiosAsync(It.IsAny<string>(), "site-1", "device-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<List<Radio>>.Ok(radios));

        var tokens = CreateTestTokens();

        // Act
        var result = await _service.GetSiteWithDevicesAsync(tokens, "site-1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Devices);
        Assert.Single(result.Data.Devices[0].Radios);
    }

    [Fact]
    public async Task ToggleRadioAsync_CallsApiClientCorrectly()
    {
        // Arrange
        var expectedResponse = new RadioControlResponse(true, "Success", null);

        _apiClientMock
            .Setup(x => x.ControlRadioAsync(
                It.IsAny<string>(),
                "site-1",
                It.Is<RadioControlRequest>(r => r.DeviceId == "device-1" && r.RadioId == "radio-1" && r.Enabled == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<RadioControlResponse>.Ok(expectedResponse));

        var tokens = CreateTestTokens();

        // Act
        var result = await _service.ToggleRadioAsync(tokens, "site-1", "device-1", "radio-1", false);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Success);

        _apiClientMock.Verify(x => x.ControlRadioAsync(
            "test-access-token",
            "site-1",
            It.Is<RadioControlRequest>(r => r.Enabled == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAuthenticationAsync_ReturnsNewTokens()
    {
        // Arrange
        var loginResponse = new LoginResponse(
            "new-access-token",
            "new-refresh-token",
            3600,
            "Bearer"
        );

        _apiClientMock
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<LoginResponse>.Ok(loginResponse));

        // Act
        var result = await _service.RefreshAuthenticationAsync("old-refresh-token");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("new-access-token", result.Data.AccessToken);
    }

    private static AuthTokens CreateTestTokens() => new()
    {
        AccessToken = "test-access-token",
        RefreshToken = "test-refresh-token",
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    };
}
