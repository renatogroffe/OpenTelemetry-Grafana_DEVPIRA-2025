using APIContagem;
using APIContagem.Data;
using APIContagem.Tracing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        builder.Configuration["Loki:Uri"]!,
        new List<LokiLabel>()
        {
            new()
            {
                Key = "service_name",
                Value = OpenTelemetryExtensions.ServiceName
            },
            new()
            {
                Key = "using_database",
                Value = "true"
            }
        })
    .Enrich.WithSpan(new SpanOptions() { IncludeOperationName = true, IncludeTags = true })
    .CreateLogger());

builder.Services.AddDbContext<ContagemContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagem"),
        o => o.UseNodaTime());
});

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion);
builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddOtlpExporter()
            .AddConsoleExporter();
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ContagemRepository>();
builder.Services.AddSingleton<Contador>();

builder.Services.AddCors();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "API de Contagem";
    options.Theme = ScalarTheme.BluePlanet;
    options.DarkMode = true;
});

app.UseAuthorization();

app.MapControllers();

app.Run();