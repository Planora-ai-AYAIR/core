using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Planora.Application.Common.Behaviours;

namespace Planora.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
        var assembly = typeof(AssemblyMarker).Assembly;

        // 1. MediatR: scans for Handlers, Pre-processors (LoggingBehaviour), etc.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Open generic pipeline behaviors (FIFO order)
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            // cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>)); // optional
        });

        // 2. FluentValidation: scans for all AbstractValidator<T> implementations
        services.AddValidatorsFromAssembly(assembly);

        return services;
	}
}
