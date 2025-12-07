using Devlooped.Agents.AI;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tomlyn.Extensions.Configuration;

#if WEB
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
#endif

static class ConfigureOpenTelemetryExtensions
{
    const string HealthEndpointPath = "/health";
    const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        // .env/secrets override other config, which may contain dummy API keys, for example
        builder.Configuration
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>();

        builder.ConfigureReload();

#if WEB
        builder.AddDefaultHealthChecks();
#endif

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var serviceName = builder.Environment.ApplicationName
            ?? throw new InvalidOperationException("Application name is not set in the hosting environment.");

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName))
            .WithTracing(tracing =>
            {
#if WEB
                tracing.AddAspNetCoreInstrumentation(tracing =>
                    // Don't trace requests to the health endpoint to avoid filling the dashboard with noise
                    tracing.Filter = httpContext =>
                        !(httpContext.Request.Path.StartsWithSegments(HealthEndpointPath)
                          || httpContext.Request.Path.StartsWithSegments(AlivenessEndpointPath)));
#endif
                tracing.AddHttpClientInstrumentation();

                // Only add console exporter if explicitly enabled in configuration
                if (builder.Configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter"))
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
#if WEB
                metrics.AddAspNetCoreInstrumentation();
#endif
                metrics.AddRuntimeInstrumentation();
                metrics.AddHttpClientInstrumentation();
            });


        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    /// <summary>
    /// Configures automatic configuration reload from either the build output directory (production) 
    /// or from the project directory (development).
    /// </summary>
    public static TBuilder ConfigureReload<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (builder.Environment.IsProduction())
        {
            foreach (var toml in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.toml", SearchOption.AllDirectories))
                builder.Configuration.AddTomlFile(toml, optional: false, reloadOnChange: true);

            foreach (var json in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.json", SearchOption.AllDirectories))
                builder.Configuration.AddJsonFile(json, optional: false, reloadOnChange: true);

            foreach (var md in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.md", SearchOption.AllDirectories))
                builder.Configuration.AddAgentMarkdown(md, optional: false, reloadOnChange: true);
        }
        else
        {
            var baseDir = ThisAssembly.Project.MSBuildProjectDirectory;
            var outDir = Path.Combine(baseDir, ThisAssembly.Project.BaseOutputPath);
            var objDir = Path.Combine(baseDir, ThisAssembly.Project.BaseIntermediateOutputPath);

            // Only use configs outside of bin/ and obj/ directories since we want reload to happen from source files not output files
            bool IsSource(string path) => !path.StartsWith(outDir) && !path.StartsWith(objDir);

            foreach (var toml in Directory.EnumerateFiles(baseDir, "*.toml", SearchOption.AllDirectories).Where(IsSource))
                builder.Configuration.AddTomlFile(toml, optional: false, reloadOnChange: true);

            foreach (var json in Directory.EnumerateFiles(baseDir, "*.json", SearchOption.AllDirectories).Where(IsSource))
                builder.Configuration.AddJsonFile(json, optional: false, reloadOnChange: true);

            foreach (var md in Directory.EnumerateFiles(baseDir, "*.md", SearchOption.AllDirectories).Where(IsSource))
                builder.Configuration.AddAgentMarkdown(md, optional: false, reloadOnChange: true);
        }

        return builder;
    }

#if WEB
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
           where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
#endif
}
