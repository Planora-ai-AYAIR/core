using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Planora.Infrastructure.Options;

namespace Planora.Api.Extensions;

public static class PresentationExtensions
{
    public static IServiceCollection AddPresentationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- OpenAPI (Swashbuckle generates the doc, Scalar provides the UI) ---
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // --- Health Checks ---
        services.AddHealthChecks().AddNpgSql(configuration.GetConnectionString("DefaultConnection")!);

        return services;
    }
}
