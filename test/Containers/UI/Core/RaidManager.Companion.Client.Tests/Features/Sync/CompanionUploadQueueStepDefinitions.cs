using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Defines business-readable steps for the queue of snapshots waiting to upload.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Verifies #550's queue and its owner decisions: the newest snapshot of a character replaces an older one,
/// an accepted snapshot isn't queued again, the queue survives a restart, and excluding an account drops its snapshots.
/// The queue's file is in a temporary folder.
/// </remarks>
[Binding]
[Scope(Feature = "Companion upload queue")]
public sealed class CompanionUploadQueueStepDefinitions : IDisposable
{
    #region Fields
    /// <summary>Stores the folder holding the queue's file.</summary>
    private readonly string _folder = Snapshots.TemporaryFolder();

    /// <summary>Stores the queue under test.</summary>
    private SnapshotQueue _queue;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionUploadQueueStepDefinitions"/> class.</summary>
    public CompanionUploadQueueStepDefinitions()
    {
        _queue = Snapshots.Queue(_folder);
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose() => Snapshots.DeleteFolder(_folder);
    #endregion Public Methods

    #region Given Steps
    /// <summary>Queues a snapshot.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="capturedAt">The capture time.</param>
    [Given("a snapshot of {string} captured at {int} is waiting")]
    public void GivenASnapshotOfCapturedAtIsWaiting(string name, int capturedAt) =>
        _queue.Offer(Snapshots.Queued(name, capturedAt)).ShouldBeTrue();

    /// <summary>Queues a snapshot from an account.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="capturedAt">The capture time.</param>
    /// <param name="account">The account.</param>
    [Given("a snapshot of {string} captured at {int} is waiting from the account {string}")]
    public void GivenASnapshotOfCapturedAtIsWaitingFromTheAccount(string name, int capturedAt, string account) =>
        _queue.Offer(Snapshots.Queued(name, capturedAt, account)).ShouldBeTrue();

    /// <summary>Queues a snapshot and has RaidManager accept it.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="capturedAt">The capture time.</param>
    [Given("a snapshot of {string} captured at {int} was accepted")]
    public void GivenASnapshotOfCapturedAtWasAccepted(string name, int capturedAt)
    {
        var snapshot = Snapshots.Queued(name, capturedAt);
        _queue.Offer(snapshot).ShouldBeTrue();
        _queue.Accept(snapshot, DateTimeOffset.UnixEpoch);
    }

    /// <summary>Reopens the queue from its file.</summary>
    [Given("the companion has restarted")]
    public void GivenTheCompanionHasRestarted() => Restart();
    #endregion Given Steps

    #region When Steps
    /// <summary>Offers a snapshot read from the addon's file.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="capturedAt">The capture time.</param>
    [When("a snapshot of {string} captured at {int} is read")]
    public void WhenASnapshotOfCapturedAtIsRead(string name, int capturedAt) => _queue.Offer(Snapshots.Queued(name, capturedAt));

    /// <summary>Reopens the queue from its file.</summary>
    [When("the companion restarts")]
    public void WhenTheCompanionRestarts() => Restart();

    /// <summary>Drops an account's snapshots.</summary>
    /// <param name="account">The account.</param>
    [When("the player excludes the account {string}")]
    public void WhenThePlayerExcludesTheAccount(string account) => _queue.DropAccount(account);
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the waiting snapshots, oldest first.</summary>
    /// <param name="waiting">The snapshots as <c>Name at time</c>, comma-separated, or <c>none</c>.</param>
    [Then("the waiting snapshots are {string}")]
    public void ThenTheWaitingSnapshotsAre(string waiting) => Snapshots.Describe(_queue).ShouldBe(waiting);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Opens a new queue on the same file, as a new start of the companion does.</summary>
    private void Restart() => _queue = Snapshots.Queue(_folder);
    #endregion Private Helpers
}
