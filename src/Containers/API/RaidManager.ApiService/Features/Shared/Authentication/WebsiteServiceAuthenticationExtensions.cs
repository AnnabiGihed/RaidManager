namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Registers the website-service authentication scheme and authorization policy.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Fails the host at startup when the website key is missing or short, so the API never runs open.
/// </remarks>
public static class WebsiteServiceAuthenticationExtensions
{
    #region Public Methods
    /// <summary>Adds the website-service scheme and the <see cref="WebsiteServiceDefaults.Policy"/> policy.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding <see cref="WebsiteServiceDefaults.ServiceKeyConfigurationKey"/>.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the website key is missing or shorter than the minimum.</exception>
    public static IServiceCollection AddWebsiteServiceAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceKey = configuration[WebsiteServiceDefaults.ServiceKeyConfigurationKey];
        if (serviceKey is null || serviceKey.Length < WebsiteServiceDefaults.MinimumServiceKeyLength)
        {
            throw new InvalidOperationException(
                $"Configure '{WebsiteServiceDefaults.ServiceKeyConfigurationKey}' with at least {WebsiteServiceDefaults.MinimumServiceKeyLength} characters; the Aspire AppHost generates it.");
        }

        services.AddAuthentication(WebsiteServiceDefaults.Scheme)
            .AddScheme<ServiceKeyAuthenticationOptions, ServiceKeyAuthenticationHandler>(
                WebsiteServiceDefaults.Scheme,
                options => options.ServiceKey = serviceKey);
        services.AddAuthorizationBuilder().AddPolicy(WebsiteServiceDefaults.Policy, policy => policy
            .AddAuthenticationSchemes(WebsiteServiceDefaults.Scheme)
            .RequireClaim(WebsiteServiceDefaults.ServiceClaim, WebsiteServiceDefaults.WebsiteService));
        return services;
    }
    #endregion Public Methods
}
