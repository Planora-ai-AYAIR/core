using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Planora.Application;

public static class DependancyInjection
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		var applicationAssembly = typeof(AssemblyMarker).Assembly;

		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
		services.AddValidatorsFromAssembly(applicationAssembly);

		return services;
	}
}
