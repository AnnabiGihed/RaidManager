using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using RaidManager.Companion.Client.Configuration;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Shared;
using RaidManager.Companion.Client.Features.Tokens;
using RaidManager.Companion.Client.Features.Tray;
using RaidManager.Companion.Composition;
using RaidManager.Companion.Features.Shared;
using RaidManager.Companion.Features.Shell;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Composition;

/// <summary>Verifies the companion's composition root and its environments.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Each environment's settings file gives the addresses of ADR-0032, every root view model resolves, and the
/// shell and launcher are the host's; a missing settings file fails at start, not at the first call.
/// </remarks>
public sealed class CompanionHostTests
{
    #region Tests
    /// <summary>Each environment reads its own addresses.</summary>
    /// <param name="environment">The environment.</param>
    /// <param name="api">The expected API address.</param>
    /// <param name="website">The expected website address.</param>
    [Theory]
    [InlineData("Development", "https://localhost:55366/", "https://localhost:55365/")]
    [InlineData("Dev", "https://api.raidmanager-dev.pivotsoftwares.com/", "https://raidmanager-dev.pivotsoftwares.com/")]
    [InlineData("Test", "https://api.raidmanager-test.pivotsoftwares.com/", "https://raidmanager-test.pivotsoftwares.com/")]
    [InlineData("Production", "https://api.raidmanager.pivotsoftwares.com/", "https://raidmanager.pivotsoftwares.com/")]
    public void BuildReadsTheEnvironmentsAddresses(string environment, string api, string website)
    {
        using var host = CompanionHost.Build(new ApplicationShell(null), environment);

        var options = host.Services.GetRequiredService<IOptions<CompanionOptions>>().Value;

        options.ApiBaseUrl.ShouldBe(new Uri(api));
        options.WebsiteBaseUrl.ShouldBe(new Uri(website));
    }

    /// <summary>Every root view model resolves, over the host's shell and launcher; the sync loop runs on Windows only.</summary>
    [Fact]
    public void BuildResolvesTheViewModels()
    {
        var shell = new ApplicationShell(null);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Companion:ApiBaseUrl"] = "https://api.raidmanager.test/",
                ["Companion:WebsiteBaseUrl"] = "https://raidmanager.test/",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCompanion(configuration, shell);
        if (!OperatingSystem.IsWindows())
        {
            services.AddSingleton(Mock.Of<ITokenProtector>());
        }

        using var provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetRequiredService<PairingViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<TrayViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<IApplicationShell>().ShouldBeSameAs(shell);
        provider.GetRequiredService<IBrowserLauncher>().ShouldBeOfType<AvaloniaBrowserLauncher>();
        provider.GetRequiredService<ITokenProtector>().ShouldNotBeNull();
        provider.GetRequiredService<KeepsRunningNoticePresenter>().ShouldNotBeNull();
        services.Count(service => service.ServiceType == typeof(IHostedService)).ShouldBe(OperatingSystem.IsWindows() ? 1 : 0);
    }

    /// <summary>An environment without a settings file fails when the host is built.</summary>
    [Fact]
    public void BuildWithoutTheEnvironmentsFileFails() =>
        Should.Throw<FileNotFoundException>(() => CompanionHost.Build(new ApplicationShell(null), "Missing"));

    /// <summary>The build names its environment, and an assembly that names none is Development.</summary>
    [Fact]
    public void EnvironmentComesFromTheBuild()
    {
        CompanionEnvironment.Of(typeof(App).Assembly).ShouldBe("Development");
        CompanionEnvironment.Of(typeof(object).Assembly).ShouldBe("Development");
        Should.Throw<ArgumentNullException>(() => CompanionEnvironment.Of(null!));
    }

    /// <summary>A started host validates the addresses.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StartedHostValidatesTheAddresses()
    {
        using var host = CompanionHost.Build(new ApplicationShell(null), "Development");

        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
    #endregion Tests
}
