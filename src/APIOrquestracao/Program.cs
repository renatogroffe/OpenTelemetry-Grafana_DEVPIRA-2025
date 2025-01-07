using APIOrquestracao.Clients;
using APIOrquestracao.Tracing;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Sinks.Grafana.Loki;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<ContagemClient>();

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
                Value = "false"
            }
        })
    .Enrich.WithSpan(new SpanOptions() { IncludeOperationName = true, IncludeTags = true })
    .CreateLogger());

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
                Value = "false"
            }
        })
    .Enrich.WithSpan(new SpanOptions() { IncludeOperationName = true, IncludeTags = true })
    .CreateLogger());

builder.Services.AddOpenTelemetry().WithTracing(traceProvider =>
{
    traceProvider
        .AddSource(OpenTelemetryExtensions.ServiceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
        .AddConsoleExporter();
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "API de Orquestracao";
    options.Theme = ScalarTheme.DeepSpace;
    options.DarkMode = true;
});

app.UseAuthorization();

app.MapControllers();

app.Run();
