using Microsoft.Extensions.Logging.Abstractions;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Settings;
using RaidManager.Companion.Client.Features.Sync.Watch;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the sync's settings file and the watch of an addon file in temporary folders.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Technical tests of #550: the settings survive a restart, a damaged settings file starts a new search, the
/// settings keep account hashes only, a file WoW holds open isn't read, and a file that disappears is no change.
/// </remarks>
public sealed class SyncFilesTests : IDisposable
{
    #region Fields
    /// <summary>Stores the test's temporary folder.</summary>
    private readonly string _folder = Snapshots.TemporaryFolder();

    /// <summary>Stores the time of the scans.</summary>
    private readonly DateTimeOffset _now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    #endregion Fields

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose() => Snapshots.DeleteFolder(_folder);
    #endregion Public Methods

    #region Tests
    /// <summary>Saved settings load back as they were.</summary>
    [Fact]
    public void SettingsLoadBackAsSaved()
    {
        var settings = new SyncSettings(["C:/Games/WoW"], ["D:/WoW"], ["0F3A"], Paused: true);
        Store().Save(settings);

        var loaded = Store().Load();

        loaded.FoundFolders.ShouldBe(["C:/Games/WoW"]);
        loaded.AddedFolders.ShouldBe(["D:/WoW"]);
        loaded.ExcludedAccounts.ShouldBe(["0F3A"]);
        loaded.Paused.ShouldBeTrue();
        Directory.GetFiles(_folder).Select(Path.GetFileName).ShouldBe(["sync-settings.json"]);
    }

    /// <summary>A damaged settings file counts as a companion that hasn't searched yet.</summary>
    [Fact]
    public void ADamagedSettingsFileStartsANewSearch()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(new SyncFileLocation(_folder).SettingsPath, "{ not json");

        Store().Load().ShouldBe(SyncSettings.Initial);
    }

    /// <summary>The watched folders list each folder once, found or added.</summary>
    [Fact]
    public void FoldersListEachFolderOnce() =>
        new SyncSettings(["C:/WoW"], ["c:/wow", "D:/WoW"], [], Paused: false).Folders.ShouldBe(["C:/WoW", "D:/WoW"]);

    /// <summary>An account's hash doesn't depend on the case or a final separator of its path.</summary>
    [Fact]
    public void AccountIdsIgnoreCaseAndFinalSeparators() =>
        WowInstallationFinder.AccountId(Path.Combine(_folder, "ARTHASACC") + Path.DirectorySeparatorChar)
            .ShouldBe(WowInstallationFinder.AccountId(Path.Combine(_folder, "arthasacc")));

    /// <summary>A file WoW holds open for writing is read only once WoW lets go of it.</summary>
    [Fact]
    public void AFileWowHoldsOpenIsNotRead()
    {
        var watch = WatchOf("one-character");
        watch.Check(_now).ShouldBeNull();

        using (new FileStream(watch.Account.SavedVariablesPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            watch.Check(_now.AddSeconds(5)).ShouldBeNull();
            watch.IsWriting.ShouldBeTrue();
        }

        watch.Check(_now.AddSeconds(10)).ShouldNotBeNull().Final.ShouldBeTrue();
    }

    /// <summary>A file that disappears is no change, and its account isn't writing.</summary>
    [Fact]
    public void AMissingFileIsNoChange()
    {
        var watch = WatchOf("one-character");
        watch.Check(_now).ShouldBeNull();
        File.Delete(watch.Account.SavedVariablesPath);

        watch.Check(_now.AddSeconds(5)).ShouldBeNull();
        watch.IsWriting.ShouldBeFalse();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Opens the settings store on the test's folder.</summary>
    /// <returns>The store.</returns>
    private SyncSettingsStore Store() => new(new SyncFileLocation(_folder), NullLogger<SyncSettingsStore>.Instance);

    /// <summary>Copies a fixture under an account folder and watches it.</summary>
    /// <param name="fixture">The fixture.</param>
    /// <returns>The watch.</returns>
    private SavedVariablesWatch WatchOf(string fixture)
    {
        var account = new WowAccount("0F3A", "ARTHASACCOUNT", Path.Combine(_folder, "ARTHASACCOUNT"), 1);
        Directory.CreateDirectory(Path.GetDirectoryName(account.SavedVariablesPath)!);
        File.Copy(AddonFixtures.PathOf(fixture, "ARTHASACCOUNT"), account.SavedVariablesPath);
        return new SavedVariablesWatch(account);
    }
    #endregion Private Helpers
}
