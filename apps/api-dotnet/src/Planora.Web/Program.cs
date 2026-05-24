using Planora.Application;
using Planora.Infrastructure;
using Planora.Presentation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddApplicationPart(typeof(Planora.Presentation.DependencyInjection).Assembly);

builder.Services.AddOpenApi();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
);

var app = builder.Build();



if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
