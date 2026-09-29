using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>Provides shared Aspire service defaults for Warmane Raid Manager hosts.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Centralizes service discovery, resilience, health checks and OpenTelemetry host configuration.
/// </remarks>
public static class Extensions
{
    #region Public Methods
    /// <summary>Adds the shared service defaults used by deployable hosts.</summary>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same builder for fluent composition.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        builder.Services.AddHealthChecks().AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
        return builder;
    }

    /// <summary>Maps shared health endpoints for a web application.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application for fluent composition.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = registration => registration.Name == "self" });
        return app;
    }
    #endregion Public Methods

    #region Private Methods
    /// <summary>Configures shared OpenTelemetry instrumentation.</summary>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <param name="builder">The application builder.</param>
    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddOpenTelemetry();

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());
    }
    #endregion Private Methods
}
