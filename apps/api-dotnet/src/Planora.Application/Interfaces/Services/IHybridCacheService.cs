using Planora.Application.Common.Options;

namespace Planora.Application.Interfaces.Services;

public interface IHybridCacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken ct = default);

    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task RemoveByTagAsync(string tag, CancellationToken ct = default);
}
