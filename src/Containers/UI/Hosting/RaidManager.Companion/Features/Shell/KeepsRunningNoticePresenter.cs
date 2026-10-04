using Avalonia.Controls;
using RaidManager.Companion.Client.Features.Notices;

namespace RaidManager.Companion.Features.Shell;

/// <summary>Shows the keeps-running notice the first time the window hides to the tray.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Applies the first-close rule of #528 with the notice window of board 21: shown once, recorded at once, and
/// never again, across restarts.
/// </remarks>
internal sealed class KeepsRunningNoticePresenter
{
    #region Fields
    /// <summary>Stores the record of whether the notice was shown.</summary>
    private readonly KeepsRunningNotice _notice;

    /// <summary>Stores the clock the notice window waits on.</summary>
    private readonly TimeProvider _time;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="KeepsRunningNoticePresenter"/> class.</summary>
    /// <param name="notice">The record of whether the notice was shown.</param>
    /// <param name="time">The clock the notice window waits on.</param>
    public KeepsRunningNoticePresenter(KeepsRunningNotice notice, TimeProvider time)
    {
        _notice = notice;
        _time = time;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Shows the notice if the window has never been closed before.</summary>
    /// <returns>The notice window when it was shown; otherwise <see langword="null"/>.</returns>
    public Window? ShowOnFirstClose()
    {
        if (!_notice.IsFirstClose)
        {
            return null;
        }

        _notice.RecordShown();
        var window = new KeepsRunningNoticeWindow(_time);
        window.Show();
        return window;
    }
    #endregion Public Methods
}
