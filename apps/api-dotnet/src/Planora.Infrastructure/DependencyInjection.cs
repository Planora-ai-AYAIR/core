using Planora.Infrastructure.Presistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Planora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionMode = configuration["ConnectionMode"];
        var connectionString = connectionMode == "Prod"
            ? configuration.GetConnectionString("ProdCS")
            : configuration.GetConnectionString("DevCS");

        services.AddDbContext<PlanoraDbContext>(options =>
            options.UseSqlServer(connectionString)
        );
        return services;
    }

}
