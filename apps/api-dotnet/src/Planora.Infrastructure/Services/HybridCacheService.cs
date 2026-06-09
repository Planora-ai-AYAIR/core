using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Options;
using Planora.Application.Interfaces.Services;
using Planora.Infrastructure.Options;

namespace Planora.Infrastructure.Services;

public sealed class HybridCacheService : IHybridCacheService
{
    private readonly HybridCache _cache;
    private readonly CacheOptions _options;

    public HybridCacheService(HybridCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (!_options.Enabled)
        {
            return await factory(ct);
        }

        var entryOptions = CreateEntryOptions(options);
        var tags = NormalizeTags(options?.Tags);

        return await _cache.GetOrCreateAsync(
            key,
            async token => await factory(token),
            entryOptions,
            tags,
            ct);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_options.Enabled)
        {
            return;
        }

        var entryOptions = CreateEntryOptions(options);
        var tags = NormalizeTags(options?.Tags);

        await _cache.SetAsync(key, value, entryOptions, tags, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_options.Enabled)
        {
            return;
        }

        await _cache.RemoveAsync(key, ct);
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        if (!_options.Enabled)
        {
            return;
        }

        if (tag == "*")
        {
            await _cache.RemoveByTagAsync(tag, ct);
            return;
        }

        await _cache.RemoveByTagAsync(tag, ct);
    }

    private HybridCacheEntryOptions CreateEntryOptions(CacheEntryOptions? options)
    {
        return new HybridCacheEntryOptions
        {
            Expiration = options?.Expiration ?? _options.DefaultExpiration,
            LocalCacheExpiration = options?.LocalCacheExpiration ?? _options.DefaultLocalCacheExpiration
        };
    }

    private static IReadOnlyCollection<string>? NormalizeTags(IReadOnlyCollection<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return null;
        }

        var normalizedTags = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .ToArray();

        if (normalizedTags.Length == 0)
        {
            return null;
        }

        if (normalizedTags.Any(tag => tag == "*"))
        {
            throw new ArgumentException("Cache entry tags cannot use the reserved wildcard value '*'.", nameof(tags));
        }

        return normalizedTags;
    }
}
