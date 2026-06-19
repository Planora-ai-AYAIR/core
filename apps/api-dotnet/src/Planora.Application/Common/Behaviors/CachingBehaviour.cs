
using MediatR;

using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Interfaces;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results.Abstractions;

namespace Planora.Application.Common.Behaviours;

public class CachingBehavior<TRequest, TResponse>(
    IHybridCacheService cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {

        logger.LogInformation("Checking cache for {RequestName}", typeof(TRequest).Name);

        var cacheResult = await cache.GetAsync<TResponse>(
            key : request.CacheKey,
            ct: ct);
        
        string requestName = typeof(TRequest).Name;
        if(cacheResult is not null)
        {
          logger.LogInformation("Cache hit for {Resquest}", requestName);
          return cacheResult;
        }
        
        logger.LogInformation("Cache miss for {Resquest}", requestName);
        
        TResponse result = await next(ct);
        
        if (result.IsSuccess)
        {
            logger.LogInformation("Caching result for {RequestName}", requestName);
            await cache.SetAsync(
                request.CacheKey,
                result,
                new CacheEntryOptions
                {
                    Expiration = request.Expiration,
                    LocalCacheExpiration = request.LocalCacheExpiration,
                    Tags = request.Tags
                },
                ct);
        }
        

        return result;
    }
}