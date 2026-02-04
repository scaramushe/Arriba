using System.Net;
using System.Net.Http.Json;
using Arriba.Core.Models;
using Arriba.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Arriba.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IArubaService> _arubaServiceMock;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _arubaServiceMock = new Mock<IArubaService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing IArubaService registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IArubaService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add the mock
                services.AddScoped(_ => _arubaServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var tokens = new AuthTokens
        {
            AccessToken = "test-token",
            RefreshToken = "test-refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer"
        };

        _arubaServiceMock
            .Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AuthTokens>.Ok(tokens));

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "test@example.com",
            password = "password"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(content);
        Assert.Equal("test-token", content.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        _arubaServiceMock
            .Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AuthTokens>.Fail("Invalid credentials", 401));

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "test@example.com",
            password = "wrong-password"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "",
            password = "password"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var tokens = new AuthTokens
        {
            AccessToken = "new-token",
            RefreshToken = "new-refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer"
        };

        _arubaServiceMock
            .Setup(x => x.RefreshAuthenticationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<AuthTokens>.Ok(tokens));

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "old-refresh-token"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(content);
        Assert.Equal("new-token", content.AccessToken);
    }

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private record LoginResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt, string TokenType);
}
