using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RaidManager.Companion.Client.Features.Pairing;
using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.Discovery;
using RaidManager.Companion.Client.Features.Sync.Settings;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Defines business-readable steps for the companion's background sync.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Verifies #550 end to end on a fake drive and a fake clock: the first search and later starts, Choose
/// folders, the complete-write wait, a cut file waiting for WoW then needing the player, the pause (owner decision:
/// reading goes on) and exclusion. The steps run the loop's step one second at a time, so no timer or thread is
/// involved; a restart builds a new sync on the same files, as a new start of the companion does.
/// </remarks>
[Binding]
[Scope(Feature = "Companion background sync")]
public sealed class CompanionBackgroundSyncStepDefinitions : IDisposable
{
    #region Fields
    /// <summary>Stores the fake drive.</summary>
    private readonly WowDrive _drive = new();

    /// <summary>Stores the folder holding the companion's files.</summary>
    private readonly string _profile = Snapshots.TemporaryFolder();

    /// <summary>Stores the API double.</summary>
    private readonly FakeSnapshotApi _api = new();

    /// <summary>Stores the token store double.</summary>
    private readonly FakeTokenStore _tokens = new();

    /// <summary>Stores the clock.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Stores the installation of each account created.</summary>
    private readonly Dictionary<string, string> _installations = new(StringComparer.Ordinal);

    /// <summary>Stores the sync under test.</summary>
    private SnapshotSync _sync;

    /// <summary>Stores whether the chosen folder was accepted.</summary>
    private bool _accepted;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionBackgroundSyncStepDefinitions"/> class.</summary>
    public CompanionBackgroundSyncStepDefinitions()
    {
        _sync = NewSync();
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Releases the sync and deletes the temporary folders.</summary>
    public void Dispose()
    {
        _sync.Dispose();
        _drive.Dispose();
        Snapshots.DeleteFolder(_profile);
    }
    #endregion Public Methods

    #region Given Steps
    /// <summary>Stores a pairing.</summary>
    [Given("this computer is paired")]
    public void GivenThisComputerIsPaired() => _tokens.Stored = new PairedCompanion(Guid.NewGuid(), "Bryn", "device-token");

    /// <summary>Installs the game with one account.</summary>
    /// <param name="folder">The folder below the drive's root.</param>
    /// <param name="account">The account folder's name.</param>
    [Given("World of Warcraft is installed in {string} with the account {string}")]
    public void GivenWorldOfWarcraftIsInstalledInWithTheAccount(string folder, string account) =>
        AddAccount(_drive.AddInstallation(folder), account);

    /// <summary>Creates account folders without the game.</summary>
    /// <param name="folder">The folder below the drive's root.</param>
    /// <param name="account">The account folder's name.</param>
    [Given("a folder {string} holds the account {string} without the game")]
    public void GivenAFolderHoldsTheAccountWithoutTheGame(string folder, string account) =>
        AddAccount(_drive.AddAccountsFolder(folder), account);

    /// <summary>Writes a fixture as an account's addon file.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <param name="fixture">The fixture.</param>
    [Given("the addon file of {string} is the {string} fixture")]
    public void GivenTheAddonFileOfIsTheFixture(string account, string fixture) =>
        _drive.WriteAddonFile(_installations[account], account, AddonFixtures.Read(fixture, account));

    /// <summary>Writes a fixture whose schema version was changed.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <param name="version">The schema version.</param>
    [Given("the addon file of {string} is written by schema version {int}")]
    public void GivenTheAddonFileOfIsWrittenBySchemaVersion(string account, int version) =>
        _drive.WriteAddonFile(_installations[account], account, AddonFixtures.Read("one-character", account)
            .Replace("[\"schemaVersion\"] = 1,", $"[\"schemaVersion\"] = {version},", StringComparison.Ordinal));

    /// <summary>Writes any text as an account's addon file.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <param name="text">The text.</param>
    [Given("the addon file of {string} holds {string}")]
    public void GivenTheAddonFileOfHolds(string account, string text) => _drive.WriteAddonFile(_installations[account], account, text);

    /// <summary>Writes the <c>multiple-accounts</c> fixture cut inside a character, as a crash during the write would.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <param name="name">The character the cut falls in.</param>
    [Given("the addon file of {string} is cut inside {string}")]
    public void GivenTheAddonFileOfIsCutInside(string account, string name)
    {
        var text = AddonFixtures.Read("multiple-accounts", account);
        _drive.WriteAddonFile(_installations[account], account, text[..(text.IndexOf($"|{name}\"]", StringComparison.Ordinal) + 300)]);
    }

    /// <summary>Writes an account's addon file again with the same text and a later time.</summary>
    /// <param name="account">The account folder's name.</param>
    [Given("WoW writes the addon file of {string} again")]
    public void GivenWoWWritesTheAddonFileOfAgain(string account) =>
        _drive.WriteAddonFile(_installations[account], account, File.ReadAllText(WowDrive.AddonFilePath(_installations[account], account)) + "\n");

    /// <summary>Runs the first step of the loop.</summary>
    /// <returns>A task that completes when the step is done.</returns>
    [Given("the companion has started syncing")]
    public Task GivenTheCompanionHasStartedSyncing() => TickAsync();

    /// <summary>Runs the loop for some time.</summary>
    /// <param name="seconds">The seconds.</param>
    /// <returns>A task that completes when the steps are done.</returns>
    [Given("the companion has synced for {int} seconds")]
    public Task GivenTheCompanionHasSyncedForSeconds(int seconds) => SyncForAsync(seconds);

    /// <summary>Pauses sync.</summary>
    /// <returns>A task that completes when the pause is saved.</returns>
    [Given("the player has paused sync")]
    public Task GivenThePlayerHasPausedSync() => _sync.PauseAsync(CancellationToken.None);

    /// <summary>Excludes an account.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    [Given("the player has excluded the account {string}")]
    public Task GivenThePlayerHasExcludedTheAccount(string account) => SetWatchedAsync(account, watched: false);

    /// <summary>Watches an account again.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    [Given("the player watches the account {string} again")]
    public Task GivenThePlayerWatchesTheAccountAgain(string account) => SetWatchedAsync(account, watched: true);

    /// <summary>Makes /companion/me report that the player asked to send every character again, now (#615).</summary>
    [Given("the player asked to send every character again")]
    public void GivenThePlayerAskedToSendEveryCharacterAgain() => _api.SyncAgainRequest = _time.GetUtcNow();
    #endregion Given Steps

    #region When Steps
    /// <summary>Runs the first step of the loop.</summary>
    /// <returns>A task that completes when the step is done.</returns>
    [When("the companion starts syncing")]
    public Task WhenTheCompanionStartsSyncing() => TickAsync();

    /// <summary>Starts a new companion on the same files and runs its first step.</summary>
    /// <returns>A task that completes when the step is done.</returns>
    [When("the companion restarts")]
    public Task WhenTheCompanionRestarts()
    {
        _sync.Dispose();
        _sync = NewSync();
        return TickAsync();
    }

    /// <summary>Searches the drives again.</summary>
    /// <returns>A task that completes when the search is done.</returns>
    [When("the player finds folders again")]
    public Task WhenThePlayerFindsFoldersAgain() => _sync.FindFoldersAgainAsync(CancellationToken.None);

    /// <summary>Chooses a folder.</summary>
    /// <param name="folder">The folder below the drive's root.</param>
    /// <returns>A task that completes when the choice is handled.</returns>
    [When("the player chooses the folder {string}")]
    public async Task WhenThePlayerChoosesTheFolder(string folder) =>
        _accepted = await _sync.AddFolderAsync(_drive.PathOf(folder), CancellationToken.None);

    /// <summary>Runs the loop for some time.</summary>
    /// <param name="seconds">The seconds.</param>
    /// <returns>A task that completes when the steps are done.</returns>
    [When("the companion syncs for {int} seconds")]
    public Task WhenTheCompanionSyncsForSeconds(int seconds) => SyncForAsync(seconds);

    /// <summary>Reads the files again, as board 5's retry does.</summary>
    /// <returns>A task that completes when the next read is scheduled.</returns>
    [When("the player reads the files again")]
    public Task WhenThePlayerReadsTheFilesAgain() => _sync.ReadAgainAsync(CancellationToken.None);

    /// <summary>Resumes sync and runs one step of the loop.</summary>
    /// <returns>A task that completes when the step is done.</returns>
    [When("the player resumes sync")]
    public async Task WhenThePlayerResumesSync()
    {
        await _sync.ResumeAsync(CancellationToken.None);
        await TickAsync();
    }

    /// <summary>Excludes an account.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    [When("the player excludes the account {string}")]
    public Task WhenThePlayerExcludesTheAccount(string account) => SetWatchedAsync(account, watched: false);
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the watched accounts.</summary>
    /// <param name="accounts">The account names, comma-separated in name order, or <c>none</c>.</param>
    [Then("the watched accounts are {string}")]
    public void ThenTheWatchedAccountsAre(string accounts)
    {
        var status = _sync.Status;
        var watched = string.Join(", ", status.Installations.SelectMany(installation => installation.Accounts)
            .Where(account => !status.ExcludedAccounts.Contains(account.Id))
            .Select(account => account.Name)
            .Order(StringComparer.Ordinal));
        (watched.Length == 0 ? "none" : watched).ShouldBe(accounts);
    }

    /// <summary>Checks whether the chosen folder was accepted.</summary>
    /// <param name="answer"><c>accepted</c> or <c>refused</c>.</param>
    [Then("the folder is {string}")]
    public void ThenTheFolderIs(string answer) => _accepted.ShouldBe(answer == "accepted");

    /// <summary>Checks the uploads RaidManager received, in order.</summary>
    /// <param name="names">The characters' names, comma-separated, or <c>none</c>.</param>
    [Then("RaidManager received {string}")]
    public void ThenRaidManagerReceived(string names)
    {
        var received = string.Join(", ", _api.Uploads.Select(upload => upload.Name));
        (received.Length == 0 ? "none" : received).ShouldBe(names);
    }

    /// <summary>Checks the number of queued snapshots the status shows.</summary>
    /// <param name="count">The number.</param>
    [Then("the status shows {int} snapshots queued")]
    public void ThenTheStatusShowsSnapshotsQueued(int count) => _sync.Status.Queued.ShouldBe(count);

    /// <summary>Checks the watched and found account counts the status shows.</summary>
    /// <param name="watched">The watched accounts.</param>
    /// <param name="found">The accounts found.</param>
    [Then("the status shows {int} of {int} accounts watched")]
    public void ThenTheStatusShowsOfAccountsWatched(int watched, int found)
    {
        var status = _sync.Status;
        (status.WatchedAccountCount, status.AccountCount).ShouldBe((watched, found));
    }

    /// <summary>Checks a character's row in the recent activity.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="state">The row's state.</param>
    [Then("the activity shows {string} as {string}")]
    public void ThenTheActivityShowsAs(string name, string state) =>
        _sync.Status.Activity.ShouldContain(row => row.Character.Name == name && row.State == Enum.Parse<CharacterActivityState>(state));

    /// <summary>Checks the problems the status shows.</summary>
    /// <param name="problems">The problems as <c>Kind</c> or <c>Kind: names</c>, comma-separated, or <c>none</c>.</param>
    [Then("the problems are {string}")]
    public void ThenTheProblemsAre(string problems)
    {
        var shown = string.Join(", ", _sync.Status.Problems.Select(problem => problem.Characters.Count == 0
            ? problem.Kind.ToString()
            : $"{problem.Kind}: {string.Join(" ", problem.Characters.Select(character => character.Name))}"));
        (shown.Length == 0 ? "none" : shown).ShouldBe(problems);
    }

    /// <summary>Checks whether sync is paused.</summary>
    /// <param name="state"><c>paused</c> or <c>running</c>.</param>
    [Then("the sync is {string}")]
    public void ThenTheSyncIs(string state) => _sync.Status.Paused.ShouldBe(state == "paused");

    /// <summary>Checks how many times the sync asked whether the player asked to sync again.</summary>
    /// <param name="times">The expected number.</param>
    [Then("the sync asked about a request {int} times")]
    public void ThenTheSyncAskedAboutARequest(int times) => _api.SyncAgainChecks.ShouldBe(times);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Builds a sync on the fake drive and the test's profile folder, as a start of the companion does.</summary>
    /// <returns>The sync.</returns>
    private SnapshotSync NewSync()
    {
        var location = new SyncFileLocation(_profile);
        var queue = Snapshots.Queue(_profile);
        var uploader = new SnapshotUploader(queue, _api, _tokens, _time, NullLogger<SnapshotUploader>.Instance, new Random(550));
        return new SnapshotSync(
            new SyncSettingsStore(location, NullLogger<SyncSettingsStore>.Instance),
            new WowInstallationFinder(_drive),
            queue,
            uploader,
            _time,
            NullLogger<SnapshotSync>.Instance);
    }

    /// <summary>Creates an account folder and remembers its installation.</summary>
    /// <param name="installation">The installation's full path.</param>
    /// <param name="account">The account folder's name.</param>
    private void AddAccount(string installation, string account)
    {
        WowDrive.AddCharacter(installation, account, Snapshots.Realm, "Arthasdk");
        _installations[account] = installation;
    }

    /// <summary>Watches or excludes an account.</summary>
    /// <param name="account">The account folder's name.</param>
    /// <param name="watched">Whether to watch it.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    private Task SetWatchedAsync(string account, bool watched) =>
        _sync.SetAccountWatchedAsync(WowDrive.AccountId(_installations[account], account), watched, CancellationToken.None);

    /// <summary>Runs one step of the loop now, then one each second for some time.</summary>
    /// <param name="seconds">The seconds.</param>
    /// <returns>A task that completes when the steps are done.</returns>
    private async Task SyncForAsync(int seconds)
    {
        await TickAsync();
        for (var second = 0; second < seconds; second++)
        {
            _time.Advance(SnapshotSync.TickInterval);
            await TickAsync();
        }
    }

    /// <summary>Runs one step of the loop.</summary>
    /// <returns>A task that completes when the step is done.</returns>
    private Task TickAsync() => _sync.TickAsync(CancellationToken.None);
    #endregion Private Helpers
}
