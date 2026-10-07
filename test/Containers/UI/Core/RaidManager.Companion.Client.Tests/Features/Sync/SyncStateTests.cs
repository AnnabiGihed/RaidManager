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
