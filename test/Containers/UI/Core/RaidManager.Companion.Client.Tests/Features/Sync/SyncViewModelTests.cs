using RaidManager.Companion.Client.Features.Sync;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the technical behavior of the sync view model.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: The status reaches the screens only on the window's thread, unsaved choices survive the sync's regular
/// checks, the texts follow the boards in their edge cases, failures stay inside the commands, and disposal stops
/// listening (#551).
/// </remarks>
public sealed class SyncViewModelTests : IDisposable
{
    #region Fields
    /// <summary>Stores the screens under test.</summary>
    private readonly SyncScreens _screens = new();
    #endregion Fields

    #region Public Methods
    /// <summary>Releases the view models.</summary>
    public void Dispose() => _screens.Dispose();
    #endregion Public Methods

    #region Tests
    /// <summary>A status raised on the sync's thread changes nothing until the window's thread runs.</summary>
    [Fact]
    public void StatusReachesTheScreensOnlyOnTheWindowsThread()
    {
        _screens.Sync.Open();
        var changed = new List<string?>();
        _screens.Sync.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _screens.SyncDouble.Change(SyncStatuses.Watching(paused: true));

        _screens.Sync.Screen.ShouldBe(SyncScreen.Syncing);
        changed.ShouldBeEmpty();
        _screens.Ui.Pending.ShouldBe(1);
        _screens.Ui.RunAll();
        _screens.Sync.Screen.ShouldBe(SyncScreen.Paused);
        changed.ShouldContain(nameof(SyncViewModel.IsPaused));
    }

    /// <summary>The player's unsaved choice survives a status with the same accounts, and goes when they change.</summary>
    [Fact]
    public void UnsavedChoicesSurviveTheSyncsChecks()
    {
        _screens.Sync.ShowFolders();
        var rows = _screens.Sync.Installations;
        rows[0].Accounts[0].IsWatched = false;

        _screens.Change(SyncStatuses.Watching(queued: 5));

        _screens.Sync.Installations.ShouldBeSameAs(rows);
        _screens.Sync.Installations[0].Accounts[0].IsWatched.ShouldBeFalse();
        _screens.Change(SyncStatuses.Empty());
        _screens.Sync.Installations.ShouldBeEmpty();
        _screens.Sync.HasNoFolders.ShouldBeTrue();
    }

    /// <summary>Ticking a folder that is already ticked keeps an account the player cleared (#593).</summary>
    [Fact]
    public void TickingAWatchedFolderChangesNothing()
    {
        _screens.Sync.ShowFolders();
        var installation = _screens.Sync.Installations[0];
        installation.Accounts[0].IsWatched = false;

        installation.IsWatched = true;

        installation.Accounts[0].IsWatched.ShouldBeFalse();
    }

    /// <summary>Saving sends only the accounts whose choice changed.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task SavingSendsOnlyTheChangedAccounts()
    {
        _screens.Sync.ShowFolders();
        var arthas = _screens.Sync.Installations[0].Accounts[0];
        arthas.IsWatched = false;
        arthas.IsWatched = true;

        await _screens.Sync.SaveCommand.ExecuteAsync();

        _screens.SyncDouble.Actions.ShouldBeEmpty();
        _screens.Sync.Screen.ShouldBe(SyncScreen.Syncing);
    }

    /// <summary>A canceled folder picker asks the sync nothing.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task CanceledPickerAddsNothing()
    {
        _screens.Picker.Choice = null;

        await _screens.Sync.AddFolderCommand.ExecuteAsync();

        _screens.Picker.Opened.ShouldBe(1);
        _screens.SyncDouble.Actions.ShouldBeEmpty();
        _screens.Sync.IsFolderRejected.ShouldBeFalse();
    }

    /// <summary>A failed action stays inside its command and leaves the board as it was.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task FailedActionKeepsTheBoard()
    {
        _screens.Sync.Open();
        _screens.SyncDouble.Failure = new IOException("The settings file is locked.");

        await _screens.Sync.PauseCommand.ExecuteAsync();

        _screens.Sync.Screen.ShouldBe(SyncScreen.Syncing);
        _screens.Sync.PauseCommand.CanExecute(null).ShouldBeTrue();
    }

    /// <summary>The "Watched folders" button shows board 1 from board 2.</summary>
    [Fact]
    public void WatchedFoldersButtonShowsBoardOne()
    {
        _screens.Sync.Open();

        _screens.Sync.ShowFoldersCommand.Execute(null);

        _screens.Sync.IsWatchedFolders.ShouldBeTrue();
        _screens.Sync.IsSyncing.ShouldBeFalse();
    }

    /// <summary>Texts before any success, with one snapshot, and on older days.</summary>
    [Fact]
    public void TextsFollowTheBoardsInTheirEdgeCases()
    {
        _screens.Change(SyncStatuses.Empty() with { Queued = 1 });
        _screens.Sync.LastSuccessText.ShouldBe("Not yet");
        _screens.Sync.QueuedText.ShouldBe("1 snapshot");
        _screens.Sync.AccountsText.ShouldBe("0 of 0 watched");

        _screens.Change(SyncStatuses.Watching() with { LastSuccess = SyncStatuses.LastSuccess.AddDays(-5) });
        _screens.Sync.LastSuccessText.ShouldBe("3 Oct, 14:05");
        _screens.Sync.Activity.Count.ShouldBe(4);
        _screens.Sync.Activity.Select(row => row.UploadedText).ShouldBe([string.Empty, string.Empty, "Uploaded today, 14:05", "Uploaded yesterday, 14:05"]);
    }

    /// <summary>A search in progress shows on board 1.</summary>
    [Fact]
    public void SearchShowsWhileItRuns()
    {
        _screens.Change(SyncStatuses.Empty() with { Searching = true });

        _screens.Sync.IsSearching.ShouldBeTrue();
    }

    /// <summary>The footer follows the pairing's player.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task FooterFollowsThePairing()
    {
        var changed = new List<string?>();
        _screens.Sync.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await _screens.StartPairedAsync();

        changed.ShouldContain(nameof(SyncViewModel.PairedText));
        _screens.Sync.PairedText.ShouldStartWith($"Paired with {SyncScreens.Player} · ");
    }

    /// <summary>Disposal stops listening to the sync.</summary>
    [Fact]
    public void DisposalStopsListening()
    {
        _screens.SyncDouble.ListenerCount().ShouldBe(1);

        _screens.Sync.Dispose();

        _screens.SyncDouble.ListenerCount().ShouldBe(0);
    }

    /// <summary>A problem whose account or characters are unknown still says what to do.</summary>
    [Fact]
    public void ProblemsWithoutDetailsStillReadWell()
    {
        var status = SyncStatuses.Watching();

        SyncProblem.For(status with { Problems = [new("GONE-id", AccountProblemKind.UnsupportedSchema, [])] })!
            .Message.ShouldBe("An account's file was written by another addon version.");
        SyncProblem.For(status with { Problems = [new("ARTHASACC-id", AccountProblemKind.IncompleteSnapshot, [])] })!
            .RetryLabel.ShouldBe("Retry ARTHASACC");
        SyncProblem.For(status).ShouldBeNull();
        Should.Throw<ArgumentNullException>(() => SyncProblem.For(null!));
    }

    /// <summary>The account problems come before the refused snapshots.</summary>
    [Fact]
    public void AccountProblemsComeFirst()
    {
        var status = SyncStatuses.Watching(
            problems: [new("ARTHASACC-id", AccountProblemKind.UnreadableFile, [])],
            refusals: [new(new CharacterKey("Lordaeron", "Jainaice"), null, SyncStatuses.LastSuccess)]);

        SyncProblem.For(status)!.Screen.ShouldBe(SyncScreen.UnreadableFile);
    }
    #endregion Tests
}
