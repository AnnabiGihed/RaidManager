using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Shouldly;
using Xunit;

namespace RaidManager.AppHost.Tests;

/// <summary>Verifies the Compose project the AppHost publishes for a deployed environment.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: Publishes the AppHost into a temporary folder, as the deploy workflow does, and checks the settings ADR-0027,
/// ADR-0028 and ADR-0029 require, and that the <c>.env</c> template carries no value, so no local secret can leak.
/// </remarks>
public sealed class ComposePublishTests : IAsyncLifetime
{
    #region Fields
    /// <summary>Stores the folder the AppHost publishes into.</summary>
    private readonly DirectoryInfo _output = Directory.CreateTempSubdirectory("raidmanager-compose-");

    /// <summary>Stores the generated Compose file.</summary>
    private string _compose = string.Empty;

    /// <summary>Stores the generated <c>.env</c> template's lines.</summary>
    private string[] _environment = [];
    #endregion Fields

    #region Public Methods
    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RaidManager_AppHost>(
            ["--operation", "publish", "--step", "publish", "--output-path", _output.FullName]);
        await using var app = await builder.BuildAsync();
        await app.RunAsync();

        _compose = await File.ReadAllTextAsync(Path.Combine(_output.FullName, "docker-compose.yaml"));
        _environment = await File.ReadAllLinesAsync(Path.Combine(_output.FullName, ".env"));
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        _output.Delete(recursive: true);
        return Task.CompletedTask;
    }
    #endregion Public Methods

    #region Tests
    /// <summary>Names the website and API after their environment, as the shared Caddy's sites expect.</summary>
    [Fact]
    public void WebsiteAndApiCarryTheNamesCaddyForwardsTo()
    {
        _compose.ShouldContain("container_name: \"raidmanager-${DEPLOY_ENVIRONMENT}-web\"");
        _compose.ShouldContain("container_name: \"raidmanager-${DEPLOY_ENVIRONMENT}-api\"");
        _compose.ShouldContain("web:\n    external: true");
    }

    /// <summary>Publishes no port, so the shared Caddy is the only way in.</summary>
    [Fact]
    public void NoServicePublishesAPort()
    {
        // Case-sensitive and indented as a service key: the HTTP_PORTS variable must not count.
        _compose.ShouldNotContain("\n    ports:", Case.Sensitive);
    }

    /// <summary>Turns on the settings #388 delivered, in every environment.</summary>
    [Fact]
    public void DeployedSettingsAreOn()
    {
        _compose.ShouldContain("ASPNETCORE_ENVIRONMENT: \"${ASPNETCORE_ENVIRONMENT}\"");
        _compose.ShouldContain("ASPNETCORE_FORWARDEDHEADERS_ENABLED: \"true\"");
        _compose.ShouldContain("DataProtection__KeysPath: \"/home/app/data-protection-keys\"");
        _compose.ShouldContain("target: \"/home/app\"\n        source: \"data-protection\"");
    }

    /// <summary>Keeps the database on a volume with a fixed name, capped as ADR-0029 plans.</summary>
    [Fact]
    public void DatabaseKeepsAStableVolumeAndItsMemoryCap()
    {
        _compose.ShouldContain("source: \"postgres-data\"");
        _compose.ShouldContain("shared_buffers=${POSTGRES_SHARED_BUFFERS}");
        _compose.ShouldContain("memory: \"${POSTGRES_MEMORY_LIMIT}\"");
        _compose.ShouldNotContain("aspire-dashboard");
    }

    /// <summary>Lists every key the deploy workflow fills, each without a value.</summary>
    [Fact]
    public void EnvironmentTemplateHasEveryKeyAndNoValue()
    {
        var entries = _environment.Where(line => line.Length > 0 && !line.StartsWith('#')).Select(line => line.Split('=', 2)).ToList();

        entries.Select(entry => entry[0]).ShouldBe(
            [
                "API_IMAGE", "API_PORT", "ASPNETCORE_ENVIRONMENT", "DEPLOY_ENVIRONMENT", "DISCORD_BOT_IMAGE", "DISCORD_BOT_TOKEN",
                "DISCORD_CLIENT_ID", "DISCORD_CLIENT_SECRET", "POSTGRES_MEMORY_LIMIT", "POSTGRES_PASSWORD", "POSTGRES_SHARED_BUFFERS",
                "WEB_IMAGE", "WEB_PORT", "WEBSITE_SERVICE_KEY",
            ],
            ignoreOrder: true);
        entries.ShouldAllBe(entry => entry[1].Length == 0);
    }
    #endregion Tests
}
