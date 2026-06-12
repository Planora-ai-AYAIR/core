using System.Net;
using System.Net.Mail;
using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Infrastructure.BackgroundJobs;
using Planora.Infrastructure.Identity;
using Planora.Infrastructure.Options;
using Planora.Infrastructure.Persistence.Contexts;
using Planora.Infrastructure.Persistence.Repositories;
using Planora.Infrastructure.Repositories;
using Planora.Infrastructure.Services;
using System.Net;
using System.Net.Mail;

namespace Planora.Infrastructure;

public static class DependancyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- AWS Setup ---
        var awsOptions = configuration.GetAWSOptions();
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        }
        services.AddDefaultAWSOptions(awsOptions);
        services.AddAWSService<IAmazonS3>();
        services.AddScoped<IStorageService, StorageService>();

        // --- Configuration Options (bound from appsettings sections) ---
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>()
            ?? new CacheOptions();

        var redisConn = configuration.GetConnectionString("RedisConnectionString");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConn;
            });
        }

        services.AddHybridCache(options =>
        {
            if (cacheOptions.MaximumKeyLength.HasValue)
            {
                options.MaximumKeyLength = cacheOptions.MaximumKeyLength.Value;
            }

            if (cacheOptions.MaximumPayloadBytes.HasValue)
            {
                options.MaximumPayloadBytes = cacheOptions.MaximumPayloadBytes.Value;
            }

            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = cacheOptions.DefaultExpiration,
                LocalCacheExpiration = cacheOptions.DefaultLocalCacheExpiration
            };
        });

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
            .AddAuthConfig()
            .AddBackgroundJobsConfig(configuration);



        return services;
    }
    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionMode = configuration.GetValue<string>("ConnectionMode");
        var connectionString = connectionMode == "Prod"
            ? configuration.GetConnectionString("ProdCS")
            : configuration.GetConnectionString("DevCS");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseNetTopologySuite();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<PlanoraDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                dataSource,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");
                    npgsqlOptions.UseNetTopologySuite();
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
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IHybridCacheService, HybridCacheService>();
        services.AddScoped<IParcelRepository, ParcelRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IProcessTopographyJob, ProcessTopographyJob>();

        return services;
    }

    private static IServiceCollection AddBackgroundJobsConfig(this IServiceCollection services, IConfiguration configuration)
    {


        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("HangfireConnectionString"))));

        services.AddHangfireServer();
        return services;
    }

}
