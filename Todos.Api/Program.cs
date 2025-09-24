using Microsoft.EntityFrameworkCore;
using Todos.Api.Data;
using Todos.Api.Repositories;
using Scalar.AspNetCore;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Todos.Api");
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var serviceName = "Todos.Api";
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceInstanceId: Environment.MachineName)
    .AddTelemetrySdk();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName))
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddMeter(serviceName)
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddRedisInstrumentation()
            .AddNpgsql();

        tracing.AddOtlpExporter(x =>
        {
            // Configure for Jaeger all-in-one container
            // x.Endpoint = new Uri("http://localhost:14268/api/traces");
            x.Endpoint = new Uri("http://localhost:4317");
        });
    });

    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeScopes = true;
        logging.IncludeFormattedMessage = true;

        logging.AddOtlpExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.Title = "Todos API";
        // By default, MapOpenApi uses /openapi/{documentName}.json
        opt.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

app.MapPrometheusScrapingEndpoint();

app.Run();
