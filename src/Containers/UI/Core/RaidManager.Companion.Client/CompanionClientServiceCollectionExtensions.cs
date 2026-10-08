using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Notices;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shell;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.Settings;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Features.Tokens;
using RaidManager.Companion.Client.Features.Tray;

namespace RaidManager.Companion.Client;

/// <summary>Registers the companion's portable services.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The client half of the companion's composition root; the host adds the platform services the view models
/// need (the token protector, the browser launcher, the application shell, the folder picker and the window's thread).
/// </remarks>
public static class CompanionClientServiceCollectionExtensions
{
    #region Fields
    /// <summary>Stores how long a call to RaidManager may take before it counts as unavailable.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    #endregion Fields

    #region Public Methods
    /// <summary>Registers the options, the API clients, the token store, the keeps-running notice, the view models and the background sync.</summary>
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
        services.AddSnapshotSync();
        services.AddSingleton<SyncViewModel>();
        services.AddSingleton<ShellViewModel>();
        return services;
    }

    /// <summary>Runs the background sync as a hosted service, so it starts and stops with the host.</summary>
    /// <param name="services">The service collection, after <see cref="AddCompanionClient"/>.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>The host calls it only where it registers a token protector: the loop reads the device token at its
    /// first upload, and the protector exists on Windows only (#550).</remarks>
    public static IServiceCollection AddSnapshotSyncLoop(this IServiceCollection services)
    {
        services.AddHostedService(provider => provider.GetRequiredService<SnapshotSync>());
        return services;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Registers the background sync of #550: folder search, settings, queue, upload client and the sync itself.</summary>
    /// <param name="services">The service collection.</param>
    private static void AddSnapshotSync(this IServiceCollection services)
    {
        services.TryAddSingleton(SyncFileLocation.ForCurrentUser());
        services.TryAddSingleton<IDriveRoots, FixedDriveRoots>();
        services.AddSingleton<WowInstallationFinder>();
        services.AddSingleton<SyncSettingsStore>();
        services.AddSingleton<SnapshotQueue>();
        services.AddSingleton<SnapshotUploader>();
        services.AddHttpClient<ISnapshotApi, SnapshotApi>((provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<CompanionOptions>>().Value.ApiBaseUrl;
            client.Timeout = RequestTimeout;
        });
        services.AddSingleton<SnapshotSync>();
        services.AddSingleton<ISnapshotSync>(provider => provider.GetRequiredService<SnapshotSync>());
    }
    #endregion Private Helpers
}
