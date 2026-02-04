using Arriba.Core.Models;

namespace Arriba.Core.Services;

public interface IArubaApiClient
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserInfo>> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<Site>>> GetSitesAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<Site>> GetSiteAsync(string accessToken, string siteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<Device>>> GetDevicesAsync(string accessToken, string siteId, CancellationToken cancellationToken = default);
    Task<ApiResponse<Device>> GetDeviceAsync(string accessToken, string siteId, string deviceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<Radio>>> GetRadiosAsync(string accessToken, string siteId, string deviceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<RadioControlResponse>> ControlRadioAsync(string accessToken, string siteId, RadioControlRequest request, CancellationToken cancellationToken = default);
}
