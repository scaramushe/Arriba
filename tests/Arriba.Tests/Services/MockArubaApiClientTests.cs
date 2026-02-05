using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Arriba.Tests.Services;

public class MockArubaApiClientTests
{
    private readonly Mock<ILogger<MockArubaApiClient>> _loggerMock;
    private readonly MockArubaApiClient _mockClient;

    public MockArubaApiClientTests()
    {
        _loggerMock = new Mock<ILogger<MockArubaApiClient>>();
        _mockClient = new MockArubaApiClient(_loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithFakeCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "password");

        // Act
        var result = await _mockClient.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("mock-access-token-12345", result.Data.AccessToken);
        Assert.Equal("mock-refresh-token-67890", result.Data.RefreshToken);
        Assert.Equal("Bearer", result.Data.TokenType);
        Assert.Equal(3600, result.Data.ExpiresIn);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidFakeCredentials_ReturnsError()
    {
        // Arrange
        var request = new LoginRequest("wrong@example.com", "wrongpassword");

        // Act
        var result = await _mockClient.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
        Assert.Contains("Invalid credentials", result.Error);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var refreshToken = "mock-refresh-token-67890";

        // Act
        var result = await _mockClient.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("mock-access-token-12345-refreshed", result.Data.AccessToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsError()
    {
        // Arrange
        var refreshToken = "invalid-token";

        // Act
        var result = await _mockClient.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task GetSitesAsync_WithValidToken_ReturnsMockSites()
    {
        // Arrange
        var accessToken = "mock-access-token-12345";

        // Act
        var result = await _mockClient.GetSitesAsync(accessToken);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Mock Site 1", result.Data[0].Name);
        Assert.Single(result.Data[0].Devices);
    }

    [Fact]
    public async Task GetSitesAsync_WithInvalidToken_ReturnsError()
    {
        // Arrange
        var accessToken = "invalid-token";

        // Act
        var result = await _mockClient.GetSitesAsync(accessToken);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task GetRadiosAsync_WithValidToken_ReturnsMockRadios()
    {
        // Arrange
        var accessToken = "mock-access-token-12345";
        var siteId = "mock-site-1";
        var deviceId = "mock-device-1";

        // Act
        var result = await _mockClient.GetRadiosAsync(accessToken, siteId, deviceId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, r => r.Band == "2.4GHz");
        Assert.Contains(result.Data, r => r.Band == "5GHz");
    }

    [Fact]
    public async Task ControlRadioAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var accessToken = "mock-access-token-12345";
        var siteId = "mock-site-1";
        var request = new RadioControlRequest(
            DeviceId: "mock-device-1",
            RadioId: "mock-radio-1",
            Enabled: false,
            Channel: null,
            TransmitPower: null
        );

        // Act
        var result = await _mockClient.ControlRadioAsync(accessToken, siteId, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Success);
        Assert.Equal("Radio updated successfully", result.Data.Message);
        Assert.NotNull(result.Data.Radio);
        Assert.False(result.Data.Radio.Enabled);
        Assert.Equal(RadioStatus.Inactive, result.Data.Radio.Status);
    }

    [Fact]
    public async Task GetUserInfoAsync_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var accessToken = "mock-access-token-12345";

        // Act
        var result = await _mockClient.GetUserInfoAsync(accessToken);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("Mock User", result.Data.Name);
    }

    [Fact]
    public async Task LoginAsync_CompletesWithoutStalling()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "password");
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var task = _mockClient.LoginAsync(request);
        var completedInTime = await Task.WhenAny(task, Task.Delay(timeout)) == task;

        // Assert
        Assert.True(completedInTime, "Login should complete within 5 seconds");
        var result = await task;
        Assert.True(result.Success);
    }
}
