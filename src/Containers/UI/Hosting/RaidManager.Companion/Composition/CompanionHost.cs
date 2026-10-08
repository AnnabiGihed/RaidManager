using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RaidManager.Companion.Client;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Tokens;
using RaidManager.Companion.Features.Shared;
using RaidManager.Companion.Features.Shell;
using RaidManager.Companion.Features.Sync;
using RaidManager.Companion.Features.Tokens;

namespace RaidManager.Companion.Composition;

/// <summary>Builds the companion's services: configuration, logging, HTTP and the view models.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The companion's single composition root (avalonia-desktop §7). It reads only the settings files next to
/// the executable, logs to the debugger only, and registers the Windows-only token protector, and the sync loop that
/// needs it, only on Windows.
/// </remarks>
internal static class CompanionHost
{
    #region Public Methods
    /// <summary>Builds the host for an environment.</summary>
    /// <param name="shell">The application shell the tray and the browser launcher use.</param>
    /// <param name="environmentName">The environment, such as <c>Dev</c>.</param>
    /// <returns>The host, not started.</returns>
    public static IHost Build(ApplicationShell shell, string environmentName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: false);
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging")).AddDebug();
        builder.Services.AddCompanion(builder.Configuration, shell);
        return builder.Build();
    }

    /// <summary>Registers the companion's portable services and the platform services of this host.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the <c>Companion</c> section.</param>
    /// <param name="shell">The application shell.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddCompanion(this IServiceCollection services, IConfiguration configuration, ApplicationShell shell)
    {
        services.AddCompanionClient(configuration);
        services.AddSingleton(shell);
        services.AddSingleton<IApplicationShell>(shell);
        services.AddSingleton<IBrowserLauncher, AvaloniaBrowserLauncher>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<IUiThread, AvaloniaUiThread>();
        services.AddSingleton<KeepsRunningNoticePresenter>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ITokenProtector, DpapiTokenProtector>();
            services.AddSnapshotSyncLoop();
        }

        return services;
    }
    #endregion Public Methods
}
