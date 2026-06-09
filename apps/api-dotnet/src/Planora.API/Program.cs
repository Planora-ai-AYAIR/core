using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Planora.Api;
using Planora.Application;
using Planora.Infrastructure;
using Planora.Infrastructure.Persistence.Seeders;
using Serilog;
using System.Text.Json;
using FluentValidation;
using Planora.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
//  Service Registration (Composition Root)
// ──────────────────────────────────────────────
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddPresentationServices(builder.Configuration);

builder.Services.AddControllers();
// Register IMiddleware implementation so it can be injected and used by UseMiddleware
builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
);

var app = builder.Build();

await AuthSeeder.SeedAsync(app.Services);

// ──────────────────────────────────────────────
//  HTTP Request Pipeline
// ──────────────────────────────────────────────

app.UseSerilogRequestLogging();

// Use the IMiddleware-based global exception handler (registered above)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/jobs", new DashboardOptions
    {
        Authorization = [
            new HangfireCustomBasicAuthenticationFilter{
                User = app.Configuration.GetValue<string>("HangfireSettings:Username"),
                Pass = app.Configuration.GetValue<string>("HangfireSettings:Password")
            }
        ],
        DashboardTitle = "Planora"
    });
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
