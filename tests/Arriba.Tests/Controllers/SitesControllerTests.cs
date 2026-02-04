using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Arriba.Tests.Controllers;

public class SitesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IArubaService> _arubaServiceMock;

    public SitesControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task GetSites_WithValidToken_ReturnsSites()
    {
        // Arrange
        var sites = new List<Site>
        {
            new("site-1", "Test Site", "Description", "UTC", new List<Device>())
        };

        _arubaServiceMock
            .Setup(x => x.GetSitesAsync(It.IsAny<AuthTokens>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<List<Site>>.Ok(sites));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.GetAsync("/api/sites");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<List<Site>>();
        Assert.NotNull(content);
        Assert.Single(content);
        Assert.Equal("Test Site", content[0].Name);
    }

    [Fact]
    public async Task GetSites_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/sites");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSite_WithValidToken_ReturnsSiteWithDevices()
    {
        // Arrange
        var device = new Device("device-1", "AP-1", "20:9c:b4:c5:dd:be", "AP11D", "SN123", DeviceStatus.Online, new List<Radio>());
        var site = new Site("site-1", "Test Site", null, null, new List<Device> { device });

        _arubaServiceMock
            .Setup(x => x.GetSiteWithDevicesAsync(It.IsAny<AuthTokens>(), "site-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<Site>.Ok(site));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.GetAsync("/api/sites/site-1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<Site>();
        Assert.NotNull(content);
        Assert.Equal("Test Site", content.Name);
        Assert.Single(content.Devices);
    }

    [Fact]
    public async Task GetDevices_ReturnsDevicesList()
    {
        // Arrange
        var devices = new List<Device>
        {
            new("device-1", "AP-1", "20:9c:b4:c5:dd:be", "AP11D", "SN123", DeviceStatus.Online, new List<Radio>())
        };

        _arubaServiceMock
            .Setup(x => x.GetDevicesAsync(It.IsAny<AuthTokens>(), "site-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<List<Device>>.Ok(devices));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.GetAsync("/api/sites/site-1/devices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<List<Device>>();
        Assert.NotNull(content);
        Assert.Single(content);
    }

    [Fact]
    public async Task GetDevice_ReturnsDeviceWithRadios()
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
        var response = await client.GetAsync("/api/sites/site-1/devices/device-1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<Device>();
        Assert.NotNull(content);
        Assert.Equal(2, content.Radios.Count);
    }
}
