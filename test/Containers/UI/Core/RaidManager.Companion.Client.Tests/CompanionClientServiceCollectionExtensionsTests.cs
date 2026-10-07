using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Features.Tokens;
using RaidManager.Companion.Client.Features.Tray;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests;

/// <summary>Verifies the client half of the companion's composition root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="CompanionClientServiceCollectionExtensions"/>: with the platform services the
/// host adds, every root view model resolves, the API client points at the configured host, and missing addresses
/// fail validation instead of the first call.
/// </remarks>
public sealed class CompanionClientServiceCollectionExtensionsTests
{
    #region Tests
    /// <summary>The root view models resolve, and the API client points at the configured host.</summary>
    [Fact]
    public void AddCompanionClientWithThePlatformServicesResolvesTheViewModels()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["Companion:ApiBaseUrl"] = "https://api.raidmanager.test/",
            ["Companion:WebsiteBaseUrl"] = "https://raidmanager.test/",
        });

        provider.GetRequiredService<PairingViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<TrayViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<ITokenStore>().ShouldBeOfType<TokenStore>();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(typeof(ICompanionApi).Name);
        client.BaseAddress.ShouldBe(new Uri("https://api.raidmanager.test/"));
        provider.GetRequiredService<ICompanionApi>().ShouldBeOfType<CompanionApi>();
    }

    /// <summary>The background sync resolves once, and runs as a hosted service only when the host asks for its loop.</summary>
    /// <param name="withLoop">Whether the host adds the loop, as it does on Windows.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheSyncRunsAsAHostedServiceOnlyWithItsLoop(bool withLoop)
    {
        using var provider = Build(
            new Dictionary<string, string?>
            {
                ["Companion:ApiBaseUrl"] = "https://api.raidmanager.test/",
                ["Companion:WebsiteBaseUrl"] = "https://raidmanager.test/",
            },
            withLoop);

        var sync = provider.GetRequiredService<ISnapshotSync>();
        provider.GetServices<IHostedService>().ShouldBe(withLoop ? [(IHostedService)sync] : []);
        provider.GetRequiredService<ISnapshotApi>().ShouldBeOfType<SnapshotApi>();
        provider.GetRequiredService<IDriveRoots>().ShouldBeOfType<FixedDriveRoots>();
    }

    /// <summary>Missing addresses fail validation.</summary>
    [Fact]
    public void AddCompanionClientWithoutAddressesFailsValidation()
    {
        using var provider = Build([]);

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CompanionOptions>>().Value);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds the services with the platform doubles the host would add.</summary>
    /// <param name="settings">The configuration values.</param>
    /// <param name="withLoop">Whether to add the sync loop, as the host does on Windows.</param>
    /// <returns>The service provider.</returns>
    private static ServiceProvider Build(Dictionary<string, string?> settings, bool withLoop = false)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Mock.Of<ITokenProtector>());
        services.AddSingleton(Mock.Of<IBrowserLauncher>());
        services.AddSingleton(Mock.Of<IApplicationShell>());
        services.AddSingleton(new SyncFileLocation(Path.Combine(Path.GetTempPath(), $"companion-sync-tests-{Guid.NewGuid():N}")));
        services.AddCompanionClient(configuration);
        if (withLoop)
        {
            services.AddSnapshotSyncLoop();
        }

        return services.BuildServiceProvider(validateScopes: true);
    }
    #endregion Private Helpers
}
