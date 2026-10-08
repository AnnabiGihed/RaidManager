using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the queue's file in a temporary folder.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Technical tests of <see cref="SnapshotQueue"/>'s file (#550): a damaged file counts as an empty queue, a
/// write leaves no temporary file, the file holds no account folder name, and dropping or accepting what isn't queued
/// changes nothing.
/// </remarks>
public sealed class SnapshotQueueTests : IDisposable
{
    #region Fields
    /// <summary>Stores the test's temporary folder.</summary>
    private readonly string _folder = Snapshots.TemporaryFolder();
    #endregion Fields

    #region Private Properties
    /// <summary>Gets the queue file's path.</summary>
    private string QueuePath => new SyncFileLocation(_folder).QueuePath;
    #endregion Private Properties

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose() => Snapshots.DeleteFolder(_folder);
    #endregion Public Methods

    #region Tests
    /// <summary>A damaged file starts an empty queue that can be written again.</summary>
    [Fact]
    public void ADamagedFileStartsAnEmptyQueue()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(QueuePath, "{ not json");

        var queue = Snapshots.Queue(_folder);

        queue.Count.ShouldBe(0);
        queue.Offer(Snapshots.Queued("Arthasdk")).ShouldBeTrue();
        Snapshots.Queue(_folder).Count.ShouldBe(1);
    }

    /// <summary>A write replaces the file and leaves no temporary file behind.</summary>
    [Fact]
    public void AWriteLeavesNoTemporaryFile()
    {
        Snapshots.Queue(_folder).Offer(Snapshots.Queued("Arthasdk"));

        Directory.GetFiles(_folder).Select(Path.GetFileName).ShouldBe(["sync-queue.json"]);
    }

    /// <summary>The file keeps the account's hash it is given, never more.</summary>
    [Fact]
    public void TheFileKeepsOnlyTheAccountIdItIsGiven()
    {
        Snapshots.Queue(_folder).Offer(Snapshots.Queued("Arthasdk", account: "0f3a"));

        var text = File.ReadAllText(QueuePath);
        text.ShouldContain("\"accountId\":\"0f3a\"");
        text.ShouldNotContain(Snapshots.Account);
    }

    /// <summary>Removing a snapshot that isn't queued changes nothing.</summary>
    [Fact]
    public void RemovingWhatIsNotQueuedChangesNothing()
    {
        var queue = Snapshots.Queue(_folder);
        queue.Offer(Snapshots.Queued("Arthasdk"));

        queue.Drop(Snapshots.Queued("Sylvanash"));
        queue.DropAccount("ALTACCOUNT").ShouldBe(0);

        queue.Count.ShouldBe(1);
    }

    /// <summary>Accepting an older snapshot keeps the newer accepted capture time.</summary>
    [Fact]
    public void AcceptingAnOlderSnapshotKeepsTheNewerCaptureTime()
    {
        var queue = Snapshots.Queue(_folder);
        queue.Accept(Snapshots.Queued("Arthasdk", 200), DateTimeOffset.UnixEpoch);
        queue.Accept(Snapshots.Queued("Arthasdk", 100), DateTimeOffset.UnixEpoch);

        queue.Offer(Snapshots.Queued("Arthasdk", 150)).ShouldBeFalse();
    }

    /// <summary>The time of an upload is kept in the file, so a restart knows when the latest upload happened (#592).</summary>
    [Fact]
    public void TheUploadTimeSurvivesARestart()
    {
        var uploadedAt = new DateTimeOffset(2026, 10, 8, 14, 5, 0, TimeSpan.Zero);
        Snapshots.Queue(_folder).Accept(Snapshots.Queued("Arthasdk", 200), uploadedAt);

        var uploaded = Snapshots.Queue(_folder).Uploaded.ShouldHaveSingleItem();

        uploaded.UploadedAt.ShouldBe(uploadedAt);
        uploaded.CapturedAt.ShouldBe(200);
    }

    /// <summary>A file written before upload times were kept still loads, without a time (#592).</summary>
    [Fact]
    public void AFileWithoutUploadTimesLoads()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(
            new SyncFileLocation(_folder).QueuePath,
            """{"queued":[],"uploaded":[{"realm":"Icecrown","name":"Arthasdk","capturedAt":200}]}""");

        var queue = Snapshots.Queue(_folder);

        queue.Uploaded.ShouldHaveSingleItem().UploadedAt.ShouldBeNull();
        queue.Offer(Snapshots.Queued("Arthasdk", 200)).ShouldBeFalse();
    }
    #endregion Tests
}
