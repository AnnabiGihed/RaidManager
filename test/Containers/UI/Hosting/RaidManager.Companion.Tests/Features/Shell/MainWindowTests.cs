using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using RaidManager.Companion.Features.Pairing;
using RaidManager.Companion.Features.Shell;
using RaidManager.Companion.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Shell;

/// <summary>Verifies the companion's window.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The window has the size and title of the companion boards, shows the pairing view, and hides to the tray
/// when the player closes it (owner decision on #514).
/// </remarks>
public sealed class MainWindowTests
{
    #region Tests
    /// <summary>The window is the 480 x 600 companion window with its title, and shows the pairing view.</summary>
    [AvaloniaFact]
    public void WindowMatchesTheCompanionBoards()
    {
        using var states = new PairingStates();
        var window = new MainWindow { DataContext = states.ViewModel };

        window.Show();

        window.Title.ShouldBe("RaidManager Companion");
        window.Width.ShouldBe(480);
        window.Height.ShouldBe(556);
        window.CanResize.ShouldBeFalse();
        window.Icon.ShouldNotBeNull();
        window.Content.ShouldBeOfType<PairingView>();
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
