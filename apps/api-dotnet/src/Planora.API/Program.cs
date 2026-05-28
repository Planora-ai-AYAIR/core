using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Planora.Api;
using Planora.Application;
using Planora.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
//  Service Registration (Composition Root)
// ──────────────────────────────────────────────
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddPresentationServices(builder.Configuration);

builder.Services.AddControllers();
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
);

var app = builder.Build();

// ──────────────────────────────────────────────
//  HTTP Request Pipeline
// ──────────────────────────────────────────────

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ──────────────────────────────────────────────
//  Endpoints
// ──────────────────────────────────────────────
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            }),
            duration = report.TotalDuration
        };

        await context.Response.WriteAsJsonAsync(result);
    }
});

app.Run();
