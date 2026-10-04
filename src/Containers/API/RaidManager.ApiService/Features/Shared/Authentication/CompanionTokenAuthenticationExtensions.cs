using Microsoft.AspNetCore.Authentication;

namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Registers the companion token scheme and authorization policy.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Adds the companion's trust beside the website's (ADR-0011, ADR-0030); the website key stays the default scheme.
/// </remarks>
public static class CompanionTokenAuthenticationExtensions
{
    #region Public Methods
    /// <summary>Adds the <see cref="CompanionTokenDefaults.Scheme"/> scheme and the <see cref="CompanionTokenDefaults.Policy"/> policy.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddCompanionTokenAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, CompanionTokenAuthenticationHandler>(CompanionTokenDefaults.Scheme, configureOptions: null);
        services.AddAuthorizationBuilder().AddPolicy(CompanionTokenDefaults.Policy, policy => policy
            .AddAuthenticationSchemes(CompanionTokenDefaults.Scheme)
            .RequireClaim(CompanionTokenDefaults.CompanionClaim));
        return services;
    }
    #endregion Public Methods
}
