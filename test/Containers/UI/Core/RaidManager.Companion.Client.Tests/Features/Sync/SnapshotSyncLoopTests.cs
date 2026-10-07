using Microsoft.Extensions.Logging.Abstractions;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Settings;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the hosted loop of the sync: its start delay, its steps and its stop.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Technical tests of <see cref="SnapshotSync"/> as a hosted service (#550): stopping right after start
/// touches nothing, the loop searches after its start delay and keeps running, a failing drive doesn't stop it, and
/// the status changes are announced. .NET 10 starts <c>ExecuteAsync</c> on a pool thread, so each test waits for the
/// loop's first delay before advancing the clock, then for each next delay, as the pairing tests do (#514).
/// </remarks>
public sealed class SnapshotSyncLoopTests : IDisposable
{
    #region Fields
    /// <summary>Stores the fake drive.</summary>
    private readonly WowDrive _drive = new();

    /// <summary>Stores the folder holding the companion's files.</summary>
    private readonly string _profile = Snapshots.TemporaryFolder();

    /// <summary>Stores the clock.</summary>
    private readonly FlowClock _time = new(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero));
    #endregion Fields

    #region Public Methods
    /// <summary>Deletes the temporary folders.</summary>
    public void Dispose()
    {
        _drive.Dispose();
        Snapshots.DeleteFolder(_profile);
    }
    #endregion Public Methods

    #region Tests
    /// <summary>A companion stopped during its start delay writes no file.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task StoppingDuringTheStartDelayTouchesNothing()
    {
        using var sync = NewSync(_drive);

        await sync.StartAsync(CancellationToken.None);
        await sync.StopAsync(CancellationToken.None);

        Directory.Exists(_profile).ShouldBeFalse();
    }

    /// <summary>After its start delay the loop searches the drives, announces it, and goes on.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task TheLoopSearchesAfterItsStartDelay()
    {
        var installation = _drive.AddInstallation("World of Warcraft");
        WowDrive.AddCharacter(installation, "ARTHASACC", Snapshots.Realm, "Arthasdk");
        using var sync = NewSync(_drive);
        var changes = 0;
        sync.StatusChanged += (_, _) => Interlocked.Increment(ref changes);

        await sync.StartAsync(CancellationToken.None);
        _time.WaitForTimers(1);
        _time.AdvanceSeconds(3, () => false);
        await sync.StopAsync(CancellationToken.None);

        sync.Status.Installations.ShouldHaveSingleItem().Folder.ShouldBe(installation);
        Volatile.Read(ref changes).ShouldBeGreaterThan(0);
    }

    /// <summary>A drive that fails to answer doesn't stop the loop.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task AFailingDriveDoesNotStopTheLoop()
    {
        using var sync = NewSync(new FailingDrive());

        await sync.StartAsync(CancellationToken.None);
        _time.WaitForTimers(1);
        _time.AdvanceSeconds(4, () => false);
        await sync.StopAsync(CancellationToken.None);

        sync.Status.Installations.ShouldBeEmpty();
    }

    /// <summary>The computer's drive list names only folders that exist.</summary>
    [Fact]
    public void FixedDrivesAreExistingFolders() =>
        new FixedDriveRoots().GetFixedDriveRoots().ShouldAllBe(root => Directory.Exists(root));
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a sync on a drive and the test's profile folder.</summary>
    /// <param name="drives">The drives to search.</param>
    /// <returns>The sync.</returns>
    private SnapshotSync NewSync(IDriveRoots drives)
    {
        var location = new SyncFileLocation(_profile);
        var queue = Snapshots.Queue(_profile);
        var uploader = new SnapshotUploader(queue, new FakeSnapshotApi(), new FakeTokenStore(), _time, NullLogger<SnapshotUploader>.Instance);
        return new SnapshotSync(
            new SyncSettingsStore(location, NullLogger<SyncSettingsStore>.Instance),
            new WowInstallationFinder(drives),
            queue,
            uploader,
            _time,
            NullLogger<SnapshotSync>.Instance);
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>A drive list that fails as a disconnected drive would.</summary>
    private sealed class FailingDrive : IDriveRoots
    {
        /// <inheritdoc />
        public IReadOnlyList<string> GetFixedDriveRoots() => throw new IOException("The device is not ready.");
    }
    #endregion Nested Types
}
