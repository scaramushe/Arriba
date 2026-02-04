namespace Arriba.Core.Models;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    int StatusCode
)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null, 200);
    public static ApiResponse<T> Fail(string error, int statusCode = 400) => new(false, default, error, statusCode);
}

public record ApiError(
    string Code,
    string Message,
    Dictionary<string, string[]>? Details = null
);
