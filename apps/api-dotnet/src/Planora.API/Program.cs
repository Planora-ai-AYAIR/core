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
using Planora.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.Local.json", optional: true, reloadOnChange: true);
}

// ──────────────────────────────────────────────
//  Service Registration (Composition Root)
// ──────────────────────────────────────────────
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddPresentationServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSignalR();
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
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/webhook"),
    builder => builder.UseMiddleware<WebhookSignatureMiddleware>());

//if (app.Environment.IsDevelopment())
//{
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
//}

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// ──────────────────────────────────────────────
//  Endpoints
// ──────────────────────────────────────────────
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
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

// http://planora-ai.runasp.net/.

app.MapGet(
    "/.well-known/acme-challenge/tkbMK-JbS7q_ncGWxTb-QSSudC9T4wN_o0rBdSRJUAo",
    () => Results.Text(
        "tkbMK-JbS7q_ncGWxTb-QSSudC9T4wN_o0rBdSRJUAo.a4MsoFTqgCOpkYthwg5QkmmkqpmHlAe3VzLxh9f1nXU",
        "text/plain"
    ));

app.Run();
