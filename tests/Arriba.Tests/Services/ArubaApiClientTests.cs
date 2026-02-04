using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Arriba.Tests.Services;

public class ArubaApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ArubaApiClient>> _loggerMock;
    private readonly ArubaApiClient _apiClient;

    public ArubaApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _loggerMock = new Mock<ILogger<ArubaApiClient>>();
        _apiClient = new ArubaApiClient(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var expectedResponse = new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiClient.LoginAsync(new LoginRequest("test@example.com", "password"));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("test-access-token", result.Data.AccessToken);
        Assert.Equal("test-refresh-token", result.Data.RefreshToken);
        Assert.Equal(3600, result.Data.ExpiresIn);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, new { error = "Invalid credentials" });

        // Act
        var result = await _apiClient.LoginAsync(new LoginRequest("test@example.com", "wrong-password"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
        Assert.Contains("Authentication failed", result.Error);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var expectedResponse = new
        {
            access_token = "new-access-token",
            refresh_token = "new-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiClient.RefreshTokenAsync("old-refresh-token");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("new-access-token", result.Data.AccessToken);
    }

    [Fact]
    public async Task GetSitesAsync_ReturnsListOfSites()
    {
        // Arrange
        var expectedResponse = new
        {
            elements = new[]
            {
                new
                {
                    id = "site-1",
                    name = "Test Site",
                    description = "A test site",
                    timezone = "America/New_York"
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiClient.GetSitesAsync("test-token");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("site-1", result.Data[0].Id);
        Assert.Equal("Test Site", result.Data[0].Name);
    }

    [Fact]
    public async Task GetDevicesAsync_ReturnsListOfDevices()
    {
        // Arrange
        var expectedResponse = new
        {
            elements = new[]
            {
                new
                {
                    id = "device-1",
                    name = "AP-1",
                    macAddress = "20:9c:b4:c5:dd:be",
                    model = "AP11D",
                    serialNumber = "SN12345",
                    status = "online"
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiClient.GetDevicesAsync("test-token", "site-1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("device-1", result.Data[0].Id);
        Assert.Equal("AP-1", result.Data[0].Name);
        Assert.Equal(DeviceStatus.Online, result.Data[0].Status);
    }

    [Fact]
    public async Task GetRadiosAsync_ReturnsListOfRadios()
    {
        // Arrange
        var expectedResponse = new
        {
            elements = new[]
            {
                new
                {
                    id = "radio-1",
                    band = "2.4GHz",
                    channel = 6,
                    channelWidth = 20,
                    transmitPower = 17,
                    enabled = true,
                    status = "active"
                },
                new
                {
                    id = "radio-2",
                    band = "5GHz",
                    channel = 36,
                    channelWidth = 40,
                    transmitPower = 20,
                    enabled = true,
                    status = "active"
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _apiClient.GetRadiosAsync("test-token", "site-1", "device-1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("2.4GHz", result.Data[0].Band);
        Assert.Equal("5GHz", result.Data[1].Band);
        Assert.True(result.Data[0].Enabled);
    }

    [Fact]
    public async Task ControlRadioAsync_ToggleRadio_ReturnsSuccess()
    {
        // Arrange
        var expectedResponse = new
        {
            id = "radio-1",
            band = "2.4GHz",
            channel = 6,
            channelWidth = 20,
            transmitPower = 17,
            enabled = false,
            status = "disabled"
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        var request = new RadioControlRequest("device-1", "radio-1", false, null, null);

        // Act
        var result = await _apiClient.ControlRadioAsync("test-token", "site-1", request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Success);
        Assert.NotNull(result.Data.Radio);
        Assert.False(result.Data.Radio.Enabled);
    }

    [Fact]
    public async Task GetSitesAsync_WithUnauthorized_ReturnsError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, new { error = "Unauthorized" });

        // Act
        var result = await _apiClient.GetSitesAsync("invalid-token");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object content)
    {
        var responseMessage = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(content)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
    }
}
