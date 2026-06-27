using MediatR;

namespace Planora.Application.Interfaces;


public interface ICachedQuery
{
    string CacheKey { get; }
    string[] Tags { get; }
    TimeSpan Expiration { get; }
    public TimeSpan? LocalCacheExpiration { get; init; }
    
}

public interface ICachedQuery<TResponse> : IRequest<TResponse>, ICachedQuery;