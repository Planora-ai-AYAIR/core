using Microsoft.EntityFrameworkCore;
using Planora.Infrastructure.Options;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Api.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Configuration Options (bound from appsettings sections) ---
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        //services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        // --- Database (PostgreSQL + EF Core) ---
        services.AddDbContext<PlanoraDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
                    })
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
