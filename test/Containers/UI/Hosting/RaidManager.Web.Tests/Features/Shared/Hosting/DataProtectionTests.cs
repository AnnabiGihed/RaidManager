using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using RaidManager.Web.Features.Shared.Hosting;
using RaidManager.Web.Tests.Support;

namespace RaidManager.Web.Tests.Features.Shared.Hosting;

/// <summary>Verifies that a deployed website keeps its Data Protection keys across a restart.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: A deployment replaces the website's container; with the keys on a volume, what one container protected,
/// such as the session cookie, another container can still read (ADR-0027).
/// </remarks>
public sealed class DataProtectionTests : IDisposable
{
    #region Fields
    /// <summary>Stores the directory standing in for the deployed volume.</summary>
    private readonly DirectoryInfo _keys = Directory.CreateTempSubdirectory("raidmanager-keys-");
    #endregion Fields

    #region Public Methods
    /// <inheritdoc />
    public void Dispose() => _keys.Delete(recursive: true);
    #endregion Public Methods

    #region Tests
    /// <summary>Protects a value in one website and reads it back in a second one started on the same directory.</summary>
    /// <returns>A task that completes when the test has run.</returns>
    [Fact]
    public async Task KeysInTheConfiguredDirectorySurviveARestart()
    {
        const string Purpose = "deployment-restart";
        string protectedValue;
        await using (var first = SiteOnTheVolume())
        {
            protectedValue = first.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Protect("session");
        }

        _keys.GetFiles("key-*.xml").ShouldNotBeEmpty();

        await using var second = SiteOnTheVolume();
        second.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose).Unprotect(protectedValue).ShouldBe("session");
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Creates a website configured, as a deployed one is, with the keys directory.</summary>
    /// <returns>The website factory.</returns>
    private WebsiteFactory SiteOnTheVolume()
    {
        var site = new WebsiteFactory();
        site.Settings[DataProtectionExtensions.KeysPathKey] = _keys.FullName;
        return site;
    }
    #endregion Private Helpers
}
