using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RaidManager.Companion.Client.Features.Notices;
using RaidManager.Companion.Features.Shell;
using RaidManager.Companion.Tests.Support;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features.Shell;

/// <summary>Verifies the keeps-running notice of board 21 and when it shows.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The notice window shows board 21's texts, closes after 6 seconds or on a click, and the presenter shows it
/// on the first close only (owner decisions on #528 and #530); the main window says when it hides to the tray.
/// </remarks>
public sealed class KeepsRunningNoticeTests : IDisposable
{
    #region Fields
    /// <summary>Stores the test's temporary folder for the marker.</summary>
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"companion-notice-ui-{Guid.NewGuid():N}");

    /// <summary>Stores the clock.</summary>
    private readonly DispatcherFlowClock _time = new(new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.Zero));
    #endregion Fields

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
    #endregion Public Methods

    #region Tests
    /// <summary>The notice shows board 21's texts in a borderless window that doesn't take the focus.</summary>
    [AvaloniaFact]
    public void NoticeShowsBoardTwentyOne()
    {
        var notice = new KeepsRunningNoticeWindow(_time);

        notice.Show();
        Dispatcher.UIThread.RunJobs();

        notice.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text)
            .ShouldBe(["RaidManager Companion", "RaidManager Companion is still running", "Quit it from its icon in the notification area."]);
        notice.WindowDecorations.ShouldBe(WindowDecorations.None);
        notice.Topmost.ShouldBeTrue();
        notice.ShowActivated.ShouldBeFalse();
        notice.ShowInTaskbar.ShouldBeFalse();
    }

    /// <summary>The notice closes on its own after 6 seconds.</summary>
    [AvaloniaFact]
    public void NoticeClosesAfterSixSeconds()
    {
        var notice = new KeepsRunningNoticeWindow(_time);
        var closed = false;
        notice.Closed += (_, _) => closed = true;
        notice.Show();

        _time.AdvanceSeconds(5, () => closed);
        closed.ShouldBeFalse();
        _time.AdvanceSeconds(1, () => closed);

        closed.ShouldBeTrue();
    }

    /// <summary>A click closes the notice at once, and the end of the 6 seconds then changes nothing.</summary>
    [AvaloniaFact]
    public void ClickClosesTheNotice()
    {
        var notice = new KeepsRunningNoticeWindow(_time);
        var closings = 0;
        notice.Closed += (_, _) => closings++;
        notice.Show();

        notice.MouseDown(new Point(100, 50), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        _time.Advance(KeepsRunningNoticeWindow.ShownFor);
        Dispatcher.UIThread.RunJobs();

        closings.ShouldBe(1);
    }

    /// <summary>The presenter shows the notice on the first close only, also after a restart.</summary>
    [AvaloniaFact]
    public void PresenterShowsTheNoticeOnce()
    {
        var location = new NoticeMarkerLocation(Path.Combine(_folder, "keeps-running-notice.shown"));

        var first = new KeepsRunningNoticePresenter(new KeepsRunningNotice(location), _time).ShowOnFirstClose();
        var second = new KeepsRunningNoticePresenter(new KeepsRunningNotice(location), _time).ShowOnFirstClose();

        first.ShouldBeOfType<KeepsRunningNoticeWindow>().IsVisible.ShouldBeTrue();
        second.ShouldBeNull();
    }

    /// <summary>Closing the main window hides it and raises the event the notice follows.</summary>
    [AvaloniaFact]
    public void MainWindowSaysWhenItHidesToTheTray()
    {
        var window = new MainWindow();
        var hidden = 0;
        window.HiddenToTray += (_, _) => hidden++;
        window.Show();

        window.Close();
        Dispatcher.UIThread.RunJobs();

        hidden.ShouldBe(1);
        window.IsVisible.ShouldBeFalse();
    }
    #endregion Tests
}
