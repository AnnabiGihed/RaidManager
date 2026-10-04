using Avalonia.Controls;

namespace RaidManager.Companion.Features.Shell;

/// <summary>The companion's window.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Hosts the pairing view. Closing the window hides it to the tray, so the companion keeps running, and says so
/// through <see cref="HiddenToTray"/> (board 21); Quit in the tray menu, or Windows shutting down, closes it for real
/// (owner decisions on #514 and #528, board 18).
/// </remarks>
public sealed partial class MainWindow : Window
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
    public MainWindow() => InitializeComponent();
    #endregion Constructors

    #region Events
    /// <summary>Occurs when the player closes the window and it hides to the tray.</summary>
    public event EventHandler? HiddenToTray;
    #endregion Events

    #region Protected Methods
    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke(this, EventArgs.Empty);
        }
    }
    #endregion Protected Methods
}
