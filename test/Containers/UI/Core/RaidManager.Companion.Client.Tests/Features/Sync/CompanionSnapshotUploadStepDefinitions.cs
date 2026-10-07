using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Sync.Queue;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Defines business-readable steps for uploading the waiting snapshots.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Verifies #550's upload rules and owner decisions: 202 and 200 remove a snapshot, 400 drops it as a
/// refusal, a failure waits 5 seconds doubling to at most 5 minutes (plus up to a fifth), 429 waits as asked, and a
/// refused token stops uploads until the companion pairs again. Uploads run one pass at a time on a fake clock, so no
/// timer or thread is involved.
/// </remarks>
[Binding]
[Scope(Feature = "Companion snapshot upload")]
public sealed class CompanionSnapshotUploadStepDefinitions : IDisposable
{
    #region Fields
    /// <summary>Stores the folder holding the queue's file.</summary>
    private readonly string _folder = Snapshots.TemporaryFolder();

    /// <summary>Stores the API double.</summary>
    private readonly FakeSnapshotApi _api = new();

    /// <summary>Stores the token store double.</summary>
    private readonly FakeTokenStore _tokens = new();

    /// <summary>Stores the clock.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Stores the queue.</summary>
    private readonly SnapshotQueue _queue;

    /// <summary>Stores the uploader under test.</summary>
    private readonly SnapshotUploader _uploader;

    /// <summary>Stores the capture time of the next snapshot queued.</summary>
    private long _capturedAt = 100;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionSnapshotUploadStepDefinitions"/> class.</summary>
    public CompanionSnapshotUploadStepDefinitions()
    {
        _queue = Snapshots.Queue(_folder);
        _uploader = new SnapshotUploader(_queue, _api, _tokens, _time, NullLogger<SnapshotUploader>.Instance, new Random(550));
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose() => Snapshots.DeleteFolder(_folder);
    #endregion Public Methods

    #region Given Steps
    /// <summary>Stores a pairing.</summary>
    [Given("this computer is paired")]
    public void GivenThisComputerIsPaired() => _tokens.Stored = new PairedCompanion(Guid.NewGuid(), "Bryn", "device-token");

    /// <summary>Stores a new pairing with another token.</summary>
    [Given("this computer is paired again")]
    public void GivenThisComputerIsPairedAgain() => _tokens.Stored = new PairedCompanion(Guid.NewGuid(), "Bryn", "new-device-token");

    /// <summary>Removes the stored pairing.</summary>
    [Given("the pairing is removed")]
    public void GivenThePairingIsRemoved() => _tokens.Stored = null;

    /// <summary>Queues a snapshot.</summary>
    /// <param name="name">The character's name.</param>
    [Given("a snapshot of {string} is waiting")]
    public void GivenASnapshotOfIsWaiting(string name) => _queue.Offer(Snapshots.Queued(name, _capturedAt++)).ShouldBeTrue();

    /// <summary>Queues a snapshot captured after the earlier ones.</summary>
    /// <param name="name">The character's name.</param>
    [Given("a newer snapshot of {string} is waiting")]
    public void GivenANewerSnapshotOfIsWaiting(string name) => GivenASnapshotOfIsWaiting(name);

    /// <summary>Makes RaidManager accept every upload with an outcome.</summary>
    /// <param name="answer">The outcome, <c>Imported</c> (202) or <c>AlreadyCurrent</c> (200); both mean accepted.</param>
    [Given("RaidManager answers uploads with {string}")]
    public void GivenRaidManagerAnswersUploadsWith(string answer)
    {
        answer.ShouldBeOneOf("Imported", "AlreadyCurrent");
        _api.Answer = new SnapshotUploadResult(SnapshotUploadOutcome.Accepted);
    }

    /// <summary>Makes RaidManager refuse a character's snapshots.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="errorCode">The problem's error code.</param>
    [Given("RaidManager refuses {string} with {string}")]
    public void GivenRaidManagerRefusesWith(string name, string errorCode) =>
        _api.AnswerFor(name, new SnapshotUploadResult(SnapshotUploadOutcome.Refused, errorCode));

    /// <summary>Makes RaidManager accept a character's snapshots again.</summary>
    /// <param name="name">The character's name.</param>
    [Given("RaidManager accepts {string} again")]
    public void GivenRaidManagerAcceptsAgain(string name) => _api.AnswerFor(name, new SnapshotUploadResult(SnapshotUploadOutcome.Accepted));

    /// <summary>Makes every upload fail as a network failure would.</summary>
    [Given("RaidManager can't be reached")]
    public void GivenRaidManagerCantBeReached() => _api.Answer = new SnapshotUploadResult(SnapshotUploadOutcome.Unavailable);

    /// <summary>Makes every upload succeed again.</summary>
    [Given("RaidManager is reachable again")]
    public void GivenRaidManagerIsReachableAgain() => _api.Answer = new SnapshotUploadResult(SnapshotUploadOutcome.Accepted);

    /// <summary>Makes RaidManager answer 429.</summary>
    /// <param name="asked">The wait it asks for, such as <c>30 seconds</c>, or <c>nothing</c> when it doesn't say.</param>
    [Given("RaidManager asks to slow down for {string}")]
    public void GivenRaidManagerAsksToSlowDownFor(string asked) =>
        _api.Answer = new SnapshotUploadResult(
            SnapshotUploadOutcome.SlowDown,
            RetryAfter: asked == "nothing" ? null : TimeSpan.FromSeconds(int.Parse(asked.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture)));

    /// <summary>Makes RaidManager refuse the device token.</summary>
    [Given("RaidManager refuses the device token")]
    public void GivenRaidManagerRefusesTheDeviceToken() => _api.RefusedToken = _tokens.Stored.ShouldNotBeNull().DeviceToken;

    /// <summary>Runs one upload pass.</summary>
    /// <returns>A task that completes when the pass is done.</returns>
    [Given("the companion has uploaded")]
    public Task GivenTheCompanionHasUploaded() => UploadAsync();
    #endregion Given Steps

    #region When Steps
    /// <summary>Runs one upload pass.</summary>
    /// <returns>A task that completes when the pass is done.</returns>
    [When("the companion uploads")]
    public Task WhenTheCompanionUploads() => UploadAsync();

    /// <summary>Runs upload passes, each when the previous wait has passed.</summary>
    /// <param name="failures">The number of passes.</param>
    /// <returns>A task that completes when the passes are done.</returns>
    [When("the companion fails to upload {int} times in a row")]
    public async Task WhenTheCompanionFailsToUploadTimesInARow(int failures)
    {
        for (var attempt = 0; attempt < failures; attempt++)
        {
            var wait = _uploader.NotBefore - _time.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                _time.Advance(wait);
            }

            await UploadAsync();
        }
    }

    /// <summary>Runs one upload pass after some time.</summary>
    /// <param name="seconds">The seconds that pass first.</param>
    /// <returns>A task that completes when the pass is done.</returns>
    [When("the companion uploads {int} seconds later")]
    public Task WhenTheCompanionUploadsSecondsLater(int seconds)
    {
        _time.Advance(TimeSpan.FromSeconds(seconds));
        return UploadAsync();
    }

    /// <summary>Retries at once, then runs one upload pass.</summary>
    /// <returns>A task that completes when the pass is done.</returns>
    [When("the player retries now")]
    public Task WhenThePlayerRetriesNow()
    {
        _uploader.RetryNow();
        return UploadAsync();
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the number of waiting snapshots.</summary>
    /// <param name="count">The number.</param>
    [Then("{int} snapshots are waiting")]
    public void ThenSnapshotsAreWaiting(int count) => _queue.Count.ShouldBe(count);

    /// <summary>Checks the connection.</summary>
    /// <param name="connection">The connection.</param>
    [Then("the connection is {string}")]
    public void ThenTheConnectionIs(string connection) => _uploader.Connection.ShouldBe(Enum.Parse<UploadConnection>(connection));

    /// <summary>Checks the last success is now.</summary>
    [Then("the last success is recorded")]
    public void ThenTheLastSuccessIsRecorded() => _uploader.LastSuccess.ShouldBe(_time.GetUtcNow());

    /// <summary>Checks the uploads RaidManager received, in order.</summary>
    /// <param name="names">The characters' names, comma-separated, or <c>none</c>.</param>
    [Then("RaidManager received {string}")]
    public void ThenRaidManagerReceived(string names)
    {
        var received = string.Join(", ", _api.Uploads.Select(upload => upload.Name));
        (received.Length == 0 ? "none" : received).ShouldBe(names);
    }

    /// <summary>Checks the refusals shown.</summary>
    /// <param name="refusals">The refusals as <c>Name: code</c>, comma-separated, or <c>none</c>.</param>
    [Then("the refusals are {string}")]
    public void ThenTheRefusalsAre(string refusals)
    {
        var shown = string.Join(", ", _uploader.Refusals.Select(refusal => $"{refusal.Character.Name}: {refusal.ErrorCode}"));
        (shown.Length == 0 ? "none" : shown).ShouldBe(refusals);
    }

    /// <summary>Checks the shortest wait before the next upload.</summary>
    /// <param name="seconds">The seconds.</param>
    [Then("the next upload waits at least {int} seconds")]
    public void ThenTheNextUploadWaitsAtLeastSeconds(int seconds) =>
        (_uploader.NotBefore - _time.GetUtcNow()).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(seconds));

    /// <summary>Checks the longest wait before the next upload.</summary>
    /// <param name="seconds">The seconds.</param>
    [Then("the next upload waits at most {int} seconds")]
    public void ThenTheNextUploadWaitsAtMostSeconds(int seconds) =>
        (_uploader.NotBefore - _time.GetUtcNow()).ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(seconds));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Runs one upload pass.</summary>
    /// <returns>A task that completes when the pass is done.</returns>
    private Task UploadAsync() => _uploader.UploadDueAsync(CancellationToken.None);
    #endregion Private Helpers
}
