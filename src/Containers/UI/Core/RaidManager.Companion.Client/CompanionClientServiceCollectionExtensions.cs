using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Notices;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Tokens;
using RaidManager.Companion.Client.Features.Tray;

namespace RaidManager.Companion.Client;

/// <summary>Registers the companion's portable services.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The client half of the companion's composition root; the host adds the platform services the view models
/// need (the token protector, the browser launcher and the application shell).
/// </remarks>
public static class CompanionClientServiceCollectionExtensions
{
    #region Fields
    /// <summary>Stores how long a call to RaidManager may take before it counts as unavailable.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    #endregion Fields

    #region Public Methods
    /// <summary>Registers the options, the API client, the token store, the keeps-running notice and the view models.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the <c>Companion</c> section.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddCompanionClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CompanionOptions>()
            .Bind(configuration.GetSection(CompanionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(TokenFileLocation.ForCurrentUser());
        services.TryAddSingleton(NoticeMarkerLocation.ForCurrentUser());
        services.AddSingleton<KeepsRunningNotice>();
        services.AddSingleton<ITokenStore, TokenStore>();
        services.AddHttpClient<ICompanionApi, CompanionApi>((provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<CompanionOptions>>().Value.ApiBaseUrl;
            client.Timeout = RequestTimeout;
        });
        services.AddSingleton<PairingViewModel>();
        services.AddSingleton<TrayViewModel>();
        return services;
    }
    #endregion Public Methods
}
