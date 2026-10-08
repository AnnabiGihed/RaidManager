using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Features.Sync.Upload;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Defines business-readable steps for the companion's sync screens.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Verifies story #17's first and third criteria on the sync view model (#551): the player reviews the
/// watched folders, excludes an account and adds a folder; the window shows the board the status calls for, with last
/// success, queued uploads, recent activity and the problems that need the player; pause, resume and the retries reach
/// the sync. The sync is a double whose status the steps set, and its changes reach the screens through the window's
/// thread.
/// </remarks>
[Binding]
[Scope(Feature = "Companion sync screens")]
public sealed class CompanionSyncScreensStepDefinitions : IDisposable
{
    #region Fields
    /// <summary>Stores the board names of the scenarios, by screen.</summary>
    private static readonly Dictionary<string, SyncScreen> Boards = new(StringComparer.Ordinal)
    {
        ["Watched folders"] = SyncScreen.WatchedFolders,
        ["Syncing"] = SyncScreen.Syncing,
        ["Paused"] = SyncScreen.Paused,
        ["Offline"] = SyncScreen.Offline,
        ["Incomplete snapshot"] = SyncScreen.IncompleteSnapshot,
        ["Refused snapshot"] = SyncScreen.RefusedSnapshot,
        ["Unsupported addon"] = SyncScreen.UnsupportedAddon,
        ["Unreadable file"] = SyncScreen.UnreadableFile,
    };

    /// <summary>Stores the character of the problem boards.</summary>
    private static readonly CharacterKey Jainaice = new("Lordaeron", "Jainaice");

    /// <summary>Stores the screens under test.</summary>
    private readonly SyncScreens _screens = new();
    #endregion Fields

    #region Public Methods
    /// <summary>Releases the view models.</summary>
    public void Dispose() => _screens.Dispose();
    #endregion Public Methods

    #region Given Steps
    /// <summary>Starts the companion with a stored pairing.</summary>
    /// <param name="player">The player.</param>
    /// <returns>A task that completes once the pairing is loaded.</returns>
    [Given("this computer is paired with {string}")]
    public Task GivenThisComputerIsPairedWith(string player)
    {
        player.ShouldBe(SyncScreens.Player);
        return _screens.StartPairedAsync();
    }

    /// <summary>Gives the sync the mockup's accounts.</summary>
    /// <param name="watched">The watched accounts, comma-separated.</param>
    /// <param name="excluded">The excluded account.</param>
    [Given("the companion found the accounts {string} and excluded {string}")]
    public void GivenTheCompanionFoundTheAccountsAndExcluded(string watched, string excluded)
    {
        var status = SyncStatuses.Watching();
        var accounts = status.Installations.SelectMany(installation => installation.Accounts).Select(account => account.Name).ToList();
        accounts.Where(name => name != excluded).ShouldBe(Names(watched));
        status.ExcludedAccounts.ShouldBe([$"{excluded}-id"]);
        _screens.Change(status);
    }

    /// <summary>Shows board 1.</summary>
    [Given("the window shows the watched folders")]
    public void GivenTheWindowShowsTheWatchedFolders() => _screens.Sync.ShowFolders();

    /// <summary>Makes the sync refuse every folder added.</summary>
    [Given("the sync refuses folders without WTF\\/Account")]
    public void GivenTheSyncRefusesFoldersWithoutWtfAccount() => _screens.SyncDouble.AcceptsFolders = false;

    /// <summary>Adds a folder through the picker.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns>A task that completes once the folder is added or refused.</returns>
    [Given("the player added the folder {string}")]
    public Task GivenThePlayerAddedTheFolder(string folder) => WhenThePlayerAddsTheFolder(folder);

    /// <summary>Puts the sync in a state of the boards.</summary>
    /// <param name="state">The state, as the scenarios name it.</param>
    [Given("the sync is {string}")]
    public void GivenTheSyncIs(string state) => _screens.Change(state switch
    {
        "uploading" => SyncStatuses.Watching(),
        "paused" => SyncStatuses.Watching(paused: true, queued: 3),
        "offline" => SyncStatuses.Watching(connection: UploadConnection.Offline, queued: 3),
        "holding an incomplete file" => SyncStatuses.Watching(problems: [new("JAINAACC-id", AccountProblemKind.IncompleteSnapshot, [Jainaice])]),
        "holding a refused snapshot" => SyncStatuses.Watching(refusals: [new(Jainaice, "Character.Snapshot.IdentityUnavailable", SyncStatuses.LastSuccess)]),
        "holding an unsupported file" => SyncStatuses.Watching(problems: [new("ARTHASACC-id", AccountProblemKind.UnsupportedSchema, [], "0.2.0")]),
        "holding an unreadable file" => SyncStatuses.Watching(problems: [new("ARTHASACC-id", AccountProblemKind.UnreadableFile, [])]),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "No such sync state in the scenarios."),
    });

    /// <summary>Gives the sync nothing found yet.</summary>
    [Given("the sync has found nothing yet")]
    public void GivenTheSyncHasFoundNothingYet() => _screens.Change(SyncStatuses.Empty());

    /// <summary>Opens the sync, as the shell does for a paired computer.</summary>
    [Given("the player opened the sync")]
    public void GivenThePlayerOpenedTheSync() => WhenThePlayerOpensTheSync();
    #endregion Given Steps

    #region When Steps
    /// <summary>Clears an account's checkbox.</summary>
    /// <param name="account">The account.</param>
    [When("the player clears {string}")]
    public void WhenThePlayerClears(string account) => Row(account).IsWatched = false;

    /// <summary>Ticks an account's checkbox.</summary>
    /// <param name="account">The account.</param>
    [When("the player ticks {string}")]
    public void WhenThePlayerTicks(string account) => Row(account).IsWatched = true;

    /// <summary>Chooses "Save and sync".</summary>
    /// <returns>A task that completes once the choices are saved.</returns>
    [When("the player saves and syncs")]
    public Task WhenThePlayerSavesAndSyncs() => _screens.Sync.SaveCommand.ExecuteAsync();

    /// <summary>Chooses "Add a folder" and picks a folder.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns>A task that completes once the folder is added or refused.</returns>
    [When("the player adds the folder {string}")]
    public Task WhenThePlayerAddsTheFolder(string folder)
    {
        _screens.Picker.Choice = folder;
        return _screens.Sync.AddFolderCommand.ExecuteAsync();
    }

    /// <summary>Chooses "Find folders again".</summary>
    /// <returns>A task that completes once the search is done.</returns>
    [When("the player finds folders again")]
    public Task WhenThePlayerFindsFoldersAgain() => _screens.Sync.FindFoldersAgainCommand.ExecuteAsync();

    /// <summary>Opens the sync.</summary>
    [When("the player opens the sync")]
    public void WhenThePlayerOpensTheSync() => _screens.Sync.Open();

    /// <summary>Chooses the retry of boards 5 to 8.</summary>
    /// <returns>A task that completes once the sync was asked.</returns>
    [When("the player retries")]
    public Task WhenThePlayerRetries() => _screens.Sync.RetryCommand.ExecuteAsync();

    /// <summary>Chooses "Pause sync".</summary>
    /// <returns>A task that completes once the sync was asked.</returns>
    [When("the player pauses sync")]
    public Task WhenThePlayerPausesSync() => _screens.Sync.PauseCommand.ExecuteAsync();

    /// <summary>Makes the sync report the pause.</summary>
    [When("the sync becomes paused")]
    public void WhenTheSyncBecomesPaused()
    {
        _screens.Change(SyncStatuses.Watching(paused: true));
        _screens.Sync.Screen.ShouldBe(SyncScreen.Paused);
    }

    /// <summary>Chooses "Resume sync".</summary>
    /// <returns>A task that completes once the sync was asked.</returns>
    [When("the player resumes sync")]
    public Task WhenThePlayerResumesSync() => _screens.Sync.ResumeCommand.ExecuteAsync();

    /// <summary>Chooses "Retry now".</summary>
    [When("the player retries now")]
    public void WhenThePlayerRetriesNow() => _screens.Sync.RetryNowCommand.Execute(null);
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts an account row's text.</summary>
    /// <param name="account">The account.</param>
    /// <param name="text">The expected text.</param>
    [Then("the row of {string} reads {string}")]
    public void ThenTheRowOfReads(string account, string text) => Row(account).CountText.ShouldBe(text);

    /// <summary>Asserts that the sync was asked nothing.</summary>
    [Then("the sync was asked nothing")]
    public void ThenTheSyncWasAskedNothing() => _screens.SyncDouble.Actions.ShouldBeEmpty();

    /// <summary>Asserts the actions the sync was asked for, in order.</summary>
    /// <param name="actions">The actions, comma-separated.</param>
    [Then("the sync was asked to {string}")]
    public void ThenTheSyncWasAskedTo(string actions) => _screens.SyncDouble.Actions.ShouldBe(Names(actions));

    /// <summary>Asserts the board shown.</summary>
    /// <param name="board">The board's name.</param>
    [Then("the window shows the {string} board")]
    public void ThenTheWindowShowsTheBoard(string board) => _screens.Sync.Screen.ShouldBe(Boards[board]);

    /// <summary>Asserts board 9.</summary>
    /// <param name="board">The board's name.</param>
    [Then("the window shows the {string} board with a warning")]
    public void ThenTheWindowShowsTheBoardWithAWarning(string board)
    {
        ThenTheWindowShowsTheBoard(board);
        _screens.Sync.IsFolderRejected.ShouldBeTrue();
    }

    /// <summary>Asserts board 1 without board 9's warning.</summary>
    /// <param name="board">The board's name.</param>
    [Then("the window shows the {string} board without a warning")]
    public void ThenTheWindowShowsTheBoardWithoutAWarning(string board)
    {
        ThenTheWindowShowsTheBoard(board);
        _screens.Sync.IsFolderRejected.ShouldBeFalse();
    }

    /// <summary>Asserts the stats card.</summary>
    /// <param name="lastSuccess">The last success.</param>
    /// <param name="queued">The queued snapshots.</param>
    /// <param name="accounts">The watched accounts.</param>
    [Then("the stats read {string}, {string} and {string}")]
    public void ThenTheStatsReadAnd(string lastSuccess, string queued, string accounts)
    {
        _screens.Sync.LastSuccessText.ShouldBe(lastSuccess);
        _screens.Sync.QueuedText.ShouldBe(queued);
        _screens.Sync.AccountsText.ShouldBe(accounts);
    }

    /// <summary>Asserts the footer.</summary>
    /// <param name="player">The player.</param>
    [Then("the footer names {string} and this computer")]
    public void ThenTheFooterNamesAndThisComputer(string player) =>
        _screens.Sync.PairedText.ShouldBe($"Paired with {player} · {Environment.MachineName}");

    /// <summary>Asserts the rows of the recent activity, in order.</summary>
    /// <param name="rows">Table with the columns <c>name</c>, <c>realm</c> and <c>state</c>.</param>
    [Then("the recent activity reads:")]
    public void ThenTheRecentActivityReads(DataTable rows) =>
        _screens.Sync.Activity.Select(row => $"{row.Name} {row.Realm} {State(row)}")
            .ShouldBe(rows.Rows.Select(row => $"{row["name"]} {row["realm"]} {row["state"]}"));

    /// <summary>Asserts the title of the problem's notice.</summary>
    /// <param name="title">The title.</param>
    [Then("the notice reads {string}")]
    public void ThenTheNoticeReads(string title) => _screens.Sync.Problem.ShouldNotBeNull().Title.ShouldBe(title);

    /// <summary>Asserts the retry button's label.</summary>
    /// <param name="label">The label.</param>
    [Then("the retry button reads {string}")]
    public void ThenTheRetryButtonReads(string label) => _screens.Sync.Problem.ShouldNotBeNull().RetryLabel.ShouldBe(label);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Splits a comma-separated list.</summary>
    /// <param name="list">The list.</param>
    /// <returns>The names.</returns>
    private static string[] Names(string list) => list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Writes a row's state as the scenarios do.</summary>
    /// <param name="row">The row.</param>
    /// <returns>The state.</returns>
    private static string State(ActivityRowViewModel row) => row switch
    {
        { IsUploading: true } => "uploading",
        { IsWaitingForWow: true } => "waiting for WoW",
        _ => row.UploadedText,
    };

    /// <summary>Finds an account row of board 1.</summary>
    /// <param name="account">The account's name.</param>
    /// <returns>The row.</returns>
    private WatchedAccountViewModel Row(string account) =>
        _screens.Sync.Installations.SelectMany(installation => installation.Accounts).Single(row => row.Name == account);
    #endregion Private Helpers
}
