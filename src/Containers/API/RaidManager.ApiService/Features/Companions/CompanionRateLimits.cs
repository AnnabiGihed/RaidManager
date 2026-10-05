using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RaidManager.ApiService.Features.Shared.Authentication;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Registers the rate-limit policies of the pairing routes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Limits pairing starts and token polls per address and code checks per player, and answers 429 as ProblemDetails. Behind the shared Caddy the address is the client's, from the forwarded headers the deployment enables.
/// </remarks>
public static class CompanionRateLimits
{
    #region Constants
    /// <summary>Defines the policy on starting a pairing, per address.</summary>
    public const string PairingStartPolicy = "companion-pairing-start";

    /// <summary>Defines the policy on polling for the token, per address.</summary>
    public const string TokenPollPolicy = "companion-token-poll";

    /// <summary>Defines the policy on looking up and confirming codes, per player.</summary>
    public const string CodeCheckPolicy = "companion-code-check";

    /// <summary>Defines the policy of snapshot uploads, partitioned by companion.</summary>
    public const string SnapshotUploadPolicy = "companion-snapshot-upload";

    /// <summary>Defines the ProblemDetails title of a refused request.</summary>
    public const string RateLimitedTitle = "RateLimit.Exceeded";

    /// <summary>Defines the partition of requests without a known address or player.</summary>
    private const string UnknownPartition = "unknown";
    #endregion Constants

    #region Public Methods
    /// <summary>Adds the options and the three policies.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding <see cref="CompanionRateLimitOptions.SectionName"/>.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddCompanionRateLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CompanionRateLimitOptions>()
            .Bind(configuration.GetSection(CompanionRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddRateLimiter(limiter =>
        {
            limiter.OnRejected = RejectAsync;
            limiter.AddPolicy(PairingStartPolicy, context => FixedWindow(
                AddressOf(context), Options(context).PairingStartsPerWindow, Options(context).PairingStartWindow));
            limiter.AddPolicy(TokenPollPolicy, context => FixedWindow(
                AddressOf(context), Options(context).TokenPollsPerWindow, Options(context).TokenPollWindow));
            limiter.AddPolicy(CodeCheckPolicy, context => FixedWindow(
                PlayerOf(context), Options(context).CodeChecksPerWindow, Options(context).CodeCheckWindow));
            limiter.AddPolicy(SnapshotUploadPolicy, context => FixedWindow(
                CompanionOf(context), Options(context).SnapshotUploadsPerWindow, Options(context).SnapshotUploadWindow));
        });
        return services;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Builds a fixed-window partition that refuses at once, without queueing.</summary>
    /// <param name="key">The partition key.</param>
    /// <param name="permits">The requests allowed per window.</param>
    /// <param name="window">The window.</param>
    /// <returns>The partition.</returns>
    private static RateLimitPartition<string> FixedWindow(string key, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });

    /// <summary>Reads the current options.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The options.</returns>
    private static CompanionRateLimitOptions Options(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<CompanionRateLimitOptions>>().Value;

    /// <summary>Partitions by the client's address.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The address, or a shared partition when unknown.</returns>
    private static string AddressOf(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? UnknownPartition;

    /// <summary>Partitions by the player in the route.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The player's identifier, or a shared partition when missing.</returns>
    private static string PlayerOf(HttpContext context) => context.GetRouteValue("userId")?.ToString() ?? UnknownPartition;

    /// <summary>Partitions by the authenticated companion, so each companion has its own upload budget.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The companion identifier, or a shared partition for an unauthenticated request.</returns>
    private static string CompanionOf(HttpContext context) =>
        context.User.FindFirst(CompanionTokenDefaults.CompanionClaim)?.Value ?? UnknownPartition;

    /// <summary>Answers a refused request with 429, ProblemDetails and, when known, how long to wait.</summary>
    /// <param name="context">The refusal.</param>
    /// <param name="cancellationToken">A token to cancel the response.</param>
    /// <returns>A task that completes when the response is written.</returns>
    private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = RateLimitedTitle,
                Detail = "Too many requests. Wait a moment and try again.",
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }
    #endregion Private Helpers
}
