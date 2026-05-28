using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planora.Infrastructure.Identity;
using Planora.Infrastructure.Options;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure;

public static class DependancyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Configuration Options (bound from appsettings sections) ---
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        //services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services
            .AddDatabase(configuration)
            .AddAuthConfig();



        return services;
    }
    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlanoraDbContext>((sp, options) =>
        {
            var connectionMode = configuration.GetValue<string>("ConnectionMode");
            var connectionString = connectionMode == "Prod"
                ? configuration.GetConnectionString("ProdCS")
                : configuration.GetConnectionString("DevCS");
            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
                    })
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }

    private static IServiceCollection AddAuthConfig(this IServiceCollection services)
    {

        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PlanoraDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

}
