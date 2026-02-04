using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Arriba.Tests.Controllers;

public class RadiosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IArubaService> _arubaServiceMock;

    public RadiosControllerTests(WebApplicationFactory<Program> factory)
    {
        _arubaServiceMock = new Mock<IArubaService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IArubaService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => _arubaServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task GetRadios_ReturnsRadiosList()
    {
        // Arrange
        var radios = new List<Radio>
        {
            new("radio-1", "2.4GHz", 6, 20, 17, true, RadioStatus.Active),
            new("radio-2", "5GHz", 36, 40, 20, true, RadioStatus.Active)
        };
        var device = new Device("device-1", "AP-1", "20:9c:b4:c5:dd:be", "AP11D", "SN123", DeviceStatus.Online, radios);

        _arubaServiceMock
            .Setup(x => x.GetDeviceWithRadiosAsync(It.IsAny<AuthTokens>(), "site-1", "device-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Device>.Ok(device));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.GetAsync("/api/sites/site-1/devices/device-1/radios");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<List<Radio>>();
        Assert.NotNull(content);
        Assert.Equal(2, content.Count);
    }

    [Fact]
    public async Task ToggleRadio_EnablesRadio_ReturnsSuccess()
    {
        // Arrange
        var radio = new Radio("radio-1", "2.4GHz", 6, 20, 17, true, RadioStatus.Active);
        var response = new RadioControlResponse(true, "Radio enabled", radio);

        _arubaServiceMock
            .Setup(x => x.ToggleRadioAsync(
                It.IsAny<AuthTokens>(),
                "site-1",
                "device-1",
                "radio-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<RadioControlResponse>.Ok(response));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            "/api/sites/site-1/devices/device-1/radios/radio-1/toggle",
            new { enabled = true });

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<RadioControlResponse>();
        Assert.NotNull(content);
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ToggleRadio_DisablesRadio_ReturnsSuccess()
    {
        // Arrange
        var radio = new Radio("radio-1", "2.4GHz", 6, 20, 17, false, RadioStatus.Disabled);
        var response = new RadioControlResponse(true, "Radio disabled", radio);

        _arubaServiceMock
            .Setup(x => x.ToggleRadioAsync(
                It.IsAny<AuthTokens>(),
                "site-1",
                "device-1",
                "radio-1",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<RadioControlResponse>.Ok(response));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var httpResponse = await client.PostAsJsonAsync(
            "/api/sites/site-1/devices/device-1/radios/radio-1/toggle",
            new { enabled = false });

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<RadioControlResponse>();
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.False(content.Radio?.Enabled);
    }

    [Fact]
    public async Task UpdateRadio_ChangesSettings_ReturnsUpdatedRadio()
    {
        // Arrange
        var radio = new Radio("radio-1", "2.4GHz", 11, 20, 20, true, RadioStatus.Active);
        var response = new RadioControlResponse(true, "Radio updated", radio);

        _arubaServiceMock
            .Setup(x => x.UpdateRadioAsync(
                It.IsAny<AuthTokens>(),
                "site-1",
                It.IsAny<RadioControlRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<RadioControlResponse>.Ok(response));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var httpResponse = await client.PatchAsJsonAsync(
            "/api/sites/site-1/devices/device-1/radios/radio-1",
            new { channel = 11, transmitPower = 20 });

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        var content = await httpResponse.Content.ReadFromJsonAsync<RadioControlResponse>();
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.Equal(11, content.Radio?.Channel);
    }

    [Fact]
    public async Task ToggleRadio_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/sites/site-1/devices/device-1/radios/radio-1/toggle",
            new { enabled = true });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ToggleRadio_WhenServiceFails_ReturnsError()
    {
        // Arrange
        _arubaServiceMock
            .Setup(x => x.ToggleRadioAsync(
                It.IsAny<AuthTokens>(),
                "site-1",
                "device-1",
                "radio-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<RadioControlResponse>.Fail("Device not responding", 503));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/sites/site-1/devices/device-1/radios/radio-1/toggle",
            new { enabled = true });

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
