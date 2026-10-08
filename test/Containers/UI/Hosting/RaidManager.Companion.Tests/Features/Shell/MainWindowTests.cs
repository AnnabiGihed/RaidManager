using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.VisualTree;
using RaidManager.Companion.Client.Features.Shell;
using RaidManager.Companion.Features.Pairing;
using RaidManager.Companion.Features.Shell;
using RaidManager.Companion.Features.Sync;
using RaidManager.Companion.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Shell;

/// <summary>Verifies the companion's window.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The window has the title of the companion boards and the size of the board it shows (owner decision on
/// #551), shows the pairing or the sync view, and hides to the tray when the player closes it (owner decision on #514).
/// </remarks>
public sealed class MainWindowTests
{
    #region Tests
    /// <summary>The window starts as the 480 x 600 pairing window with its title, and shows the pairing view.</summary>
    [AvaloniaFact]
    public void WindowMatchesThePairingBoards()
    {
        using var states = new SyncStates();
        var window = new MainWindow { DataContext = new ShellViewModel(states.Pairing, states.ViewModel) };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Title.ShouldBe("RaidManager Companion");
        window.Width.ShouldBe(480);
        window.Height.ShouldBe(556);
        window.CanResize.ShouldBeFalse();
        window.Icon.ShouldNotBeNull();
        window.GetVisualDescendants().OfType<PairingView>().ShouldHaveSingleItem();
    }

    /// <summary>Once paired, the window grows to the 560 x 680 sync boards and shows the sync view.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [AvaloniaFact]
    public async Task WindowGrowsForTheSyncBoards()
    {
        using var states = new SyncStates();
        var window = new MainWindow { DataContext = new ShellViewModel(states.Pairing, states.ViewModel) };
        window.Show();

        await states.PairAsync();
        Dispatcher.UIThread.RunJobs();

        window.Width.ShouldBe(560);
        window.Height.ShouldBe(636);
        window.GetVisualDescendants().OfType<SyncView>().ShouldHaveSingleItem();
        window.GetVisualDescendants().OfType<PairingView>().ShouldBeEmpty();
    }

    /// <summary>Closing the window hides it; the companion keeps running in the tray.</summary>
    [AvaloniaFact]
    public void ClosingHidesTheWindow()
    {
        var window = new MainWindow();
        window.Show();

        window.Close();
        Dispatcher.UIThread.RunJobs();

        window.IsVisible.ShouldBeFalse();
        window.PlatformImpl.ShouldNotBeNull();
    }
    #endregion Tests
}
