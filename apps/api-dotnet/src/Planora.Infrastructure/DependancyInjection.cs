using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;
using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Infrastructure.Identity;
using Planora.Infrastructure.Options;
using Planora.Infrastructure.Persistence.Contexts;
using Planora.Infrastructure.Repositories;
using Planora.Infrastructure.Services;

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

        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
            ?? new EmailOptions();

        services
            .AddFluentEmail(emailOptions.SenderEmail, emailOptions.SenderName)
            .AddSmtpSender(new SmtpClient(emailOptions.SmtpHost, emailOptions.SmtpPort)
            {
                Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password),
                EnableSsl = emailOptions.UseSsl
            });

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

}
