using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Moq;
using RaidManager.Companion.Features.Shared;
using RaidManager.Companion.Features.Sync;
using RaidManager.Companion.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Sync;

/// <summary>Shows each sync board headless and checks what the player sees against it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: One test per board of <c>companion-sync</c> (1 to 9): its texts in reading order, its badge, its actions;
/// and tests that a click or a key reaches the sync through the bindings (#551, avalonia-tests §3).
/// </remarks>
public sealed class SyncViewTests : IDisposable
{
    #region Constants
    /// <summary>Defines the footer of every board.</summary>
    private const string Footer = "Paired with Bryn · ";
    #endregion Constants

    #region Fields
    /// <summary>Stores the view model driver.</summary>
    private readonly SyncStates _states = new();
    #endregion Fields

    #region Public Methods
    /// <summary>Releases the view models.</summary>
    public void Dispose() => _states.Dispose();
    #endregion Public Methods

    #region Tests
    /// <summary>Board 1: the installations with their accounts, ALTACC excluded, and the three actions.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WatchedFoldersListTheAccounts()
    {
        var window = await ShowFoldersAsync();

        VisibleTexts(window).ShouldBe([
            "Watched folders",
            "RaidManager found these World of Warcraft folders. Clear\nan account to keep its characters out of RaidManager.",
            "INSTALLATION", @"C:\Games\Warmane\World of Warcraft", "✓", "ARTHASACC", "3 characters", "✓", "JAINAACC", "2 characters",
            "ALTACC", "Excluded · 4 characters", "INSTALLATION", @"D:\WoW\Warmane", "✓", "THRALLACC", "1 character",
            "Save and sync", "Find folders again", "Add a folder", Footer + Environment.MachineName]);
        VisibleButtons(window).ShouldBe(["SaveButton", "FindAgainButton", "AddFolderButton"]);
        CheckBoxes(window).Select(AutomationProperties.GetName).ShouldBe(["ARTHASACC", "JAINAACC", "ALTACC", "THRALLACC"]);
    }

    /// <summary>More installations than the window holds scroll between the intro and the buttons, which stay visible
    /// above the footer (the owner's check of #574).</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task ManyInstallationsScrollAboveTheButtons()
    {
        var window = await ShowFoldersAsync();
        _states.Change(SyncStates.ManyInstallations());
        _states.ViewModel.ShowFolders();
        Dispatcher.UIThread.RunJobs();

        var scroller = window.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Name == "InstallationScroller");
        var save = Button(window, "SaveButton");
        var footer = window.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "PairedText");
        var scrollerBottom = scroller.TranslatePoint(new Point(0, scroller.Bounds.Height), window)!.Value.Y;
        var saveTop = save.TranslatePoint(new Point(0, 0), window)!.Value.Y;
        var saveBottom = save.TranslatePoint(new Point(0, save.Bounds.Height), window)!.Value.Y;

        scroller.Extent.Height.ShouldBeGreaterThan(scroller.Viewport.Height);
        scrollerBottom.ShouldBeLessThanOrEqualTo(saveTop);
        saveBottom.ShouldBeLessThanOrEqualTo(footer.TranslatePoint(new Point(0, 0), window)!.Value.Y);
        save.IsEffectivelyVisible.ShouldBeTrue();
    }

    /// <summary>A short list keeps the buttons right under the cards, as board 1 places them.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task AShortListKeepsTheButtonsUnderTheCards()
    {
        var window = await ShowFoldersAsync();

        var scroller = window.GetVisualDescendants().OfType<ScrollViewer>().Single(viewer => viewer.Name == "InstallationScroller");
        var saveTop = Button(window, "SaveButton").TranslatePoint(new Point(0, 0), window)!.Value.Y;

        scroller.Extent.Height.ShouldBe(scroller.Viewport.Height);
        (saveTop - scroller.TranslatePoint(new Point(0, scroller.Bounds.Height), window)!.Value.Y).ShouldBe(24);
    }

    /// <summary>A click on an account's checkbox clears it, and Save and sync sends the exclusion.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task ClearingAnAccountAndSavingExcludesIt()
    {
        var window = await ShowFoldersAsync();
        var jaina = CheckBoxes(window).Single(box => AutomationProperties.GetName(box) == "JAINAACC");

        Press(window, jaina);
        Press(window, Button(window, "SaveButton"));

        jaina.IsChecked.ShouldBe(false);
        _states.Sync.Verify(sync => sync.SetAccountWatchedAsync("JAINAACC-id", false, It.IsAny<CancellationToken>()), Times.Once);
        _states.ViewModel.IsSyncing.ShouldBeTrue();
    }

    /// <summary>Board 9: "Add a folder" with a folder that isn't WoW shows the warning above the installations.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task AFolderThatIsNotWowShowsTheWarning()
    {
        var window = await ShowFoldersAsync();
        _states.Picker.Setup(picker => picker.PickFolderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(@"D:\Backups");

        Press(window, Button(window, "AddFolderButton"));

        var notice = window.GetVisualDescendants().OfType<Notice>().Single(n => n.Name == "NotWowNotice");
        notice.IsEffectivelyVisible.ShouldBeTrue();
        notice.Title.ShouldBe("This isn't a WoW folder");
        notice.Message.ShouldBe("Choose the folder that holds Wow.exe and the WTF folder.");
        _states.Sync.Verify(sync => sync.AddFolderAsync(@"D:\Backups", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Board 2: the badge, the stats, the recent activity and the two actions.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task SyncingShowsTheStatsAndTheActivity()
    {
        var window = await ShowAsync(2);

        VisibleTexts(window).ShouldBe([
            "Sync", "Syncing", "Snapshots upload when WoW finishes writing them.", "LAST SUCCESS", "Today, 14:05", "QUEUED",
            "2 snapshots", "ACCOUNTS", "3 of 4 watched", "Recent activity", "Arthasdk", "Icecrown", "Uploading", "Jainaice",
            "Lordaeron", "Waiting for WoW to finish writing", "Thrallsham", "Icecrown", "Uploaded today, 14:05", "Sylvanash",
            "Icecrown", "Uploaded today, 14:05", "Pause sync", "Watched folders", Footer + Environment.MachineName]);
        VisibleButtons(window).ShouldBe(["PauseButton", "FoldersButton"]);
        Badge(window).Classes.ShouldContain("success");
        Badge(window).Text.ShouldBe("Syncing");
        window.GetVisualDescendants().OfType<ProgressBar>().Single(bar => bar.IsEffectivelyVisible).IsIndeterminate.ShouldBeTrue();
    }

    /// <summary>Boards 3 and 4: the badge, the notice, the stats and the one action.</summary>
    /// <param name="board">The board.</param>
    /// <param name="badge">The badge's word.</param>
    /// <param name="tone">The badge's tone class, or empty for neutral.</param>
    /// <param name="intro">The line under the heading.</param>
    /// <param name="title">The notice's title.</param>
    /// <param name="button">The action's name.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaTheory]
    [InlineData(3, "Paused", "", "Nothing uploads until you resume.", "Sync is paused", "ResumeButton")]
    [InlineData(4, "Offline", "danger", "Uploads wait in the queue; nothing is lost.", "Can't reach RaidManager", "RetryNowButton")]
    public async Task PausedAndOfflineOfferOneAction(int board, string badge, string tone, string intro, string title, string button)
    {
        var window = await ShowAsync(board);

        var texts = VisibleTexts(window);
        texts.Take(3).ShouldBe(["Sync", badge, intro]);
        texts.ShouldContain(title);
        texts.ShouldContain("3 snapshots");
        VisibleButtons(window).ShouldBe([button]);
        if (tone.Length > 0)
        {
            Badge(window).Classes.ShouldContain(tone);
        }
    }

    /// <summary>Boards 5 to 8: the badge, the notice, what to do and the retry.</summary>
    /// <param name="board">The board.</param>
    /// <param name="title">The notice's title.</param>
    /// <param name="steps">The first line of what to do.</param>
    /// <param name="retry">The retry button's label.</param>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaTheory]
    [InlineData(5, "Jainaice's snapshot is incomplete", "To fix it, log in with Jainaice in WoW and type /reload, or", "Retry Jainaice")]
    [InlineData(6, "RaidManager refused Jainaice's snapshot", "To fix it, log in with Jainaice in WoW, wait a few seconds", "Retry Jainaice")]
    [InlineData(7, "This addon version isn't supported", "Update the companion, or install the addon version that", "Retry")]
    [InlineData(8, "This file isn't RaidManager's", "Reinstall the RaidManager addon, log in to WoW and type", "Retry")]
    public async Task ProblemsSayWhatToDo(int board, string title, string steps, string retry)
    {
        var window = await ShowAsync(board);

        var texts = VisibleTexts(window);
        texts.Take(2).ShouldBe(["Sync", "Needs attention"]);
        texts.ShouldContain(title);
        texts.ShouldContain(text => text != null && text.StartsWith(steps + "\n", StringComparison.Ordinal));
        texts.ShouldContain(retry);
        VisibleButtons(window).ShouldBe(["RetryButton"]);
        Badge(window).Classes.ShouldContain("warning");
    }

    /// <summary>A key on Retry asks the sync to read the files again, through the binding.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task RetryReadsTheFilesAgain()
    {
        var window = await ShowAsync(5);

        Press(window, Button(window, "RetryButton"));

        _states.Sync.Verify(sync => sync.ReadAgainAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A status raised by the sync reaches the view through the UI thread.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task StatusChangesReachTheView()
    {
        var window = await ShowAsync(2);

        _states.Change(SyncStates.Board(4));
        Dispatcher.UIThread.RunJobs();

        VisibleButtons(window).ShouldBe(["RetryNowButton"]);
    }

    /// <summary>The "Watched folders" button leads to board 1.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WatchedFoldersButtonLeadsToBoardOne()
    {
        var window = await ShowAsync(2);

        Press(window, Button(window, "FoldersButton"));

        VisibleButtons(window).ShouldBe(["SaveButton", "FindAgainButton", "AddFolderButton"]);
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Lists the texts the player sees, in reading order.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The texts.</returns>
    private static List<string?> VisibleTexts(Window window) => [.. window.GetVisualDescendants()
        .OfType<TextBlock>()
        .Where(text => text.IsEffectivelyVisible && !string.IsNullOrEmpty(text.Text))
        .Select(text => text.Text)];

    /// <summary>Lists the names of the buttons the player sees, without the checkboxes.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The names.</returns>
    private static List<string?> VisibleButtons(Window window) => [.. window.GetVisualDescendants()
        .OfType<Button>()
        .Where(button => button.IsEffectivelyVisible && button is not ToggleButton)
        .Select(button => button.Name)];

    /// <summary>Lists the checkboxes the player sees.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The checkboxes.</returns>
    private static List<CheckBox> CheckBoxes(Window window) => [.. window.GetVisualDescendants()
        .OfType<CheckBox>()
        .Where(box => box.IsEffectivelyVisible)];

    /// <summary>Finds the badge the player sees.</summary>
    /// <param name="window">The window.</param>
    /// <returns>The badge.</returns>
    private static Badge Badge(Window window) =>
        window.GetVisualDescendants().OfType<Badge>().Single(badge => badge.IsEffectivelyVisible);

    /// <summary>Finds a named button.</summary>
    /// <param name="window">The window.</param>
    /// <param name="name">The button's name.</param>
    /// <returns>The button.</returns>
    private static Button Button(Window window, string name) =>
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == name);

    /// <summary>Presses a control with the space bar, as the keyboard does.</summary>
    /// <param name="window">The window.</param>
    /// <param name="control">The control.</param>
    private static void Press(Window window, InputElement control)
    {
        control.Focus();
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Shows board 1.</summary>
    /// <returns>The window.</returns>
    private async Task<Window> ShowFoldersAsync()
    {
        var window = await ShowAsync(2);
        _states.ViewModel.ShowFolders();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>Shows the sync view in a window of the sync boards' size, on a board.</summary>
    /// <param name="board">The board, 2 to 8.</param>
    /// <returns>The window.</returns>
    private async Task<Window> ShowAsync(int board)
    {
        await _states.PairAsync();
        _states.Change(SyncStates.Board(board));
        Dispatcher.UIThread.RunJobs();
        _states.ViewModel.Open();
        var window = new Window { Width = 560, Height = 636, Content = new SyncView { DataContext = _states.ViewModel } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
    #endregion Private Helpers
}
