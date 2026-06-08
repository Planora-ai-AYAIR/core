namespace Planora.Application.Common.Options;

public sealed record CacheEntryOptions
{
    public TimeSpan? Expiration { get; init; }

    public TimeSpan? LocalCacheExpiration { get; init; }

    public IReadOnlyCollection<string>? Tags { get; init; }
}
