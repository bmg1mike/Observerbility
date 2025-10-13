using Microsoft.EntityFrameworkCore;
using Todos.Api.Data;
using Todos.Api.Repositories;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// builder.Host.UseSerilog((ctx, services, configuration) =>
// {
//     configuration
//         .ReadFrom.Configuration(ctx.Configuration)
//         .ReadFrom.Services(services)
//         .Enrich.FromLogContext()
//         .Enrich.WithProperty("Application", "Todos.Api");
// });

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

builder.AddServiceDefaults();

builder.AddSeqEndpoint(connectionName: "Seq");



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

// app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
