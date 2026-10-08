using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Features.Sync.Settings;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the parts of the sync state the scenarios don't reach: edge files and the activity list.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Technical tests of #550: files cut before or holding no schema version, a character entry that isn't a
/// table, the "Uploading" row while an upload runs, the ten-row limit of recent uploads, and "Retry now" of the sync.
/// </remarks>
public sealed class SyncStateTests : IDisposable
{
    #region Fields
    /// <summary>Stores the fake drive.</summary>
    private readonly WowDrive _drive = new();

    /// <summary>Stores the folder holding the companion's files.</summary>
    private readonly string _profile = Snapshots.TemporaryFolder();

    /// <summary>Stores the API double.</summary>
    private readonly FakeSnapshotApi _api = new();

    /// <summary>Stores the token store double, paired.</summary>
    private readonly FakeTokenStore _tokens = new() { Stored = new PairedCompanion(Guid.NewGuid(), "Bryn", "device-token") };

    /// <summary>Stores the clock.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero));
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
    /// <summary>A file cut before its schema version is incomplete; a whole file without one is unreadable.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="status">The status read.</param>
    [Theory]
    [InlineData("RaidManagerDB = { [\"addonVersion\"] = \"0.1.0\",", "Incomplete")]
    [InlineData("RaidManagerDB = { [\"addonVersion\"] = \"0.1.0\" }", "Unreadable")]
    public void AFileWithoutItsSchemaVersionGivesNoCharacter(string text, string status)
    {
        var file = SavedVariablesReader.Read(text);

        file.Status.ShouldBe(Enum.Parse<SavedVariablesReadStatus>(status));
        file.Characters.ShouldBeEmpty();
    }

    /// <summary>A character entry that isn't a table is incomplete, and the others are still read.</summary>
    [Fact]
    public void ACharacterEntryThatIsNotATableIsIncomplete()
    {
        var file = SavedVariablesReader.Read(
            "RaidManagerDB = { [\"schemaVersion\"] = 1, [\"characters\"] = { [\"Icecrown|Odd\"] = 5,"
            + " [\"Icecrown|Arthasdk\"] = { [\"realm\"] = \"Icecrown\", [\"name\"] = \"Arthasdk\", [\"capturedAt\"] = 1 } } }\n");

        file.IncompleteCharacters.ShouldBe([new CharacterKey("Icecrown", "Odd")]);
        file.Characters.ShouldHaveSingleItem().Character.Name.ShouldBe("Arthasdk");
    }

    /// <summary>While a snapshot uploads, its character is the first row of the activity.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task TheCharacterBeingUploadedLeadsTheActivity()
    {
        using var sync = NewSync(out var queue);
        queue.Offer(Snapshots.Queued("Arthasdk"));
        IReadOnlyList<CharacterActivity> during = [];
        _api.OnUpload = () => during = sync.Status.Activity;

        await sync.TickAsync(CancellationToken.None);

        during.ShouldHaveSingleItem().ShouldBe(new CharacterActivity(new CharacterKey(Snapshots.Realm, "Arthasdk"), CharacterActivityState.Uploading));
        sync.Status.Activity.ShouldHaveSingleItem().State.ShouldBe(CharacterActivityState.Uploaded);
    }

    /// <summary>The recent uploads keep the ten newest, newest first.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task RecentUploadsKeepTheTenNewest()
    {
        using var sync = NewSync(out var queue);
        for (var index = 1; index <= 11; index++)
        {
            queue.Offer(Snapshots.Queued($"Character{index}"));
        }

        await sync.TickAsync(CancellationToken.None);

        var names = sync.Status.Activity.Select(row => row.Character.Name).ToList();
        names.Count.ShouldBe(10);
        names[0].ShouldBe("Character11");
        names.ShouldNotContain("Character1");
    }

    /// <summary>"Retry now" uploads at once after a failure, without waiting.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task RetryNowUploadsAtOnce()
    {
        using var sync = NewSync(out var queue);
        queue.Offer(Snapshots.Queued("Arthasdk"));
        _api.Answer = new SnapshotUploadResult(SnapshotUploadOutcome.Unavailable);
        await sync.TickAsync(CancellationToken.None);
        _api.Answer = new SnapshotUploadResult(SnapshotUploadOutcome.Accepted);

        sync.RetryNow();
        await sync.TickAsync(CancellationToken.None);

        sync.Status.Queued.ShouldBe(0);
        sync.Status.Connection.ShouldBe(UploadConnection.Online);
    }

    /// <summary>The retry of boards 5 to 8 forgets the refused snapshots, so the screens show syncing again (#551).</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ReadingAgainForgetsTheRefusals()
    {
        using var sync = NewSync(out var queue);
        queue.Offer(Snapshots.Queued("Jainaice"));
        _api.Answer = new SnapshotUploadResult(SnapshotUploadOutcome.Refused, "Character.Snapshot.IdentityUnavailable");
        await sync.TickAsync(CancellationToken.None);
        sync.Status.Refusals.ShouldHaveSingleItem();
        var changes = 0;
        sync.StatusChanged += (_, _) => changes++;

        await sync.ReadAgainAsync(CancellationToken.None);

        sync.Status.Refusals.ShouldBeEmpty();
        changes.ShouldBeGreaterThan(0);
    }

    /// <summary>Excluding a folder stops watching its accounts and drops their waiting snapshots; watching it again
    /// brings them back (owner decision on #551).</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ExcludingAFolderLeavesItsAccountsOut()
    {
        var installation = _drive.AddInstallation("Games/WoW");
        WowDrive.AddCharacter(installation, "ARTHASACC", Snapshots.Realm, "Arthasdk");
        using var sync = NewSync(out var queue);
        await sync.TickAsync(CancellationToken.None);
        queue.Offer(Snapshots.Queued("Arthasdk", account: WowDrive.AccountId(installation, "ARTHASACC")));

        await sync.SetFolderWatchedAsync(installation.ToUpperInvariant(), false, CancellationToken.None);

        sync.Status.WatchedAccountCount.ShouldBe(0);
        sync.Status.AccountCount.ShouldBe(1);
        sync.Status.ExcludedFolders.ShouldBe([installation.ToUpperInvariant()]);
        queue.Count.ShouldBe(0);
        await sync.SetFolderWatchedAsync(installation, true, CancellationToken.None);
        sync.Status.WatchedAccountCount.ShouldBe(1);
        sync.Status.ExcludedFolders.ShouldBeEmpty();
        Should.Throw<ArgumentNullException>(() => sync.SetFolderWatchedAsync(null!, true, CancellationToken.None));
        Should.Throw<ArgumentNullException>(() => sync.Status.IsWatched(null!));
    }

    /// <summary>After a restart, the status shows the last success and the recent uploads from before it, newest
    /// first, so a sync with nothing new to send doesn't look idle (#592).</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ARestartShowsTheUploadsFromBefore()
    {
        using (var first = NewSync(out var queue))
        {
            queue.Offer(Snapshots.Queued("Arthasdk"));
            await first.TickAsync(CancellationToken.None);
            _time.Advance(TimeSpan.FromMinutes(5));
            queue.Offer(Snapshots.Queued("Jaina"));
            await first.TickAsync(CancellationToken.None);
        }

        using var later = NewSync(out _);

        later.Status.LastSuccess.ShouldBe(_time.GetUtcNow());
        later.Status.Activity.Select(row => row.Character.Name).ShouldBe(["Jaina", "Arthasdk"]);
        later.Status.Activity.ShouldAllBe(row => row.State == CharacterActivityState.Uploaded);
        later.Status.Connection.ShouldBe(UploadConnection.Unknown);
    }

    /// <summary>A later start lists the watched folders before its first step, so the window opens on the right board (#551).</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task ALaterStartListsTheWatchedFoldersAtOnce()
    {
        WowDrive.AddCharacter(_drive.AddInstallation("Games/WoW"), "ARTHASACC", Snapshots.Realm, "Arthasdk");
        using (var first = NewSync(out _))
        {
            await first.TickAsync(CancellationToken.None);
        }

        using var later = NewSync(out _);

        later.Status.WatchedAccountCount.ShouldBe(1);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Builds a sync on the fake drive and the test's profile folder.</summary>
    /// <param name="queue">The sync's queue.</param>
    /// <returns>The sync.</returns>
    private SnapshotSync NewSync(out SnapshotQueue queue)
    {
        queue = Snapshots.Queue(_profile);
        var uploader = new SnapshotUploader(queue, _api, _tokens, _time, NullLogger<SnapshotUploader>.Instance, new Random(550));
        return new SnapshotSync(
            new SyncSettingsStore(new SyncFileLocation(_profile), NullLogger<SyncSettingsStore>.Instance),
            new WowInstallationFinder(_drive),
            queue,
            uploader,
            _time,
            NullLogger<SnapshotSync>.Instance);
    }
    #endregion Private Helpers
}
