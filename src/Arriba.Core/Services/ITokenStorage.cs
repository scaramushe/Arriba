using Arriba.Core.Models;

namespace Arriba.Core.Services;

public interface ITokenStorage
{
    Task<AuthTokens?> GetTokensAsync(string userId, CancellationToken cancellationToken = default);
    Task SetTokensAsync(string userId, AuthTokens tokens, CancellationToken cancellationToken = default);
    Task RemoveTokensAsync(string userId, CancellationToken cancellationToken = default);
}

public class InMemoryTokenStorage : ITokenStorage
{
    private readonly Dictionary<string, AuthTokens> _tokens = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<AuthTokens?> GetTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _tokens.TryGetValue(userId, out var tokens) ? tokens : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetTokensAsync(string userId, AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _tokens[userId] = tokens;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _tokens.Remove(userId);
        }
        finally
        {
            _lock.Release();
        }
    }
}
