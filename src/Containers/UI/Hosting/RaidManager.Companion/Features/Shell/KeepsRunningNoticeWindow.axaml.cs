using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace RaidManager.Companion.Features.Shell;

/// <summary>The small window that tells the player the companion keeps running in the tray.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Board 21 of the companion pairing mockup, drawn by the companion (owner decision on #530): it sits at the
/// bottom right of the work area, never takes the focus, and closes after 6 seconds or when clicked. Placing and closing
/// itself is all it does.
/// </remarks>
public sealed partial class KeepsRunningNoticeWindow : Window
{
    #region Constants
    /// <summary>Defines the margin between the notice and the edges of the work area, in device-independent pixels.</summary>
    private const int EdgeMargin = 16;
    #endregion Constants

    #region Fields
    /// <summary>Stores the clock the notice waits on.</summary>
    private readonly TimeProvider _time;

    /// <summary>Stores whether the notice has closed, so the end of the wait doesn't close it twice.</summary>
    private bool _closed;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="KeepsRunningNoticeWindow"/> class, for the designer.</summary>
    public KeepsRunningNoticeWindow()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="KeepsRunningNoticeWindow"/> class.</summary>
    /// <param name="time">The clock the 6 seconds are counted on.</param>
    public KeepsRunningNoticeWindow(TimeProvider time)
    {
        _time = time;
        InitializeComponent();
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets how long the notice stays.</summary>
    public static TimeSpan ShownFor { get; } = TimeSpan.FromSeconds(6);
    #endregion Public Properties

    #region Protected Methods
    /// <inheritdoc />
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PlaceAboveTheTaskbar();
        _ = CloseLaterAsync();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Close();
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        base.OnClosed(e);
    }
    #endregion Protected Methods

    #region Private Helpers
    /// <summary>Puts the notice at the bottom right of the primary screen's work area, above the taskbar.</summary>
    private void PlaceAboveTheTaskbar()
    {
        if (Screens.Primary is not { } screen)
        {
            return;
        }

        var area = screen.WorkingArea;
        var scaling = screen.Scaling;
        Position = new PixelPoint(
            area.Right - (int)Math.Ceiling((Width + EdgeMargin) * scaling),
            area.Bottom - (int)Math.Ceiling((Height + EdgeMargin) * scaling));
    }

    /// <summary>Closes the notice once it has been shown for 6 seconds, unless it closed earlier.</summary>
    /// <returns>A task that completes when the notice closes.</returns>
    private async Task CloseLaterAsync()
    {
        await Task.Delay(ShownFor, _time);
        if (!_closed)
        {
            Close();
        }
    }
    #endregion Private Helpers
}
