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
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey),
                "JwtSettings:SecretKey is required.")
            .Validate(options => options.SecretKey.Length >= 32,
                "JwtSettings:SecretKey must be at least 32 characters.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
                "JwtSettings:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
                "JwtSettings:Audience is required.")
            .ValidateOnStart();

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
