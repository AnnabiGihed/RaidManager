using System.Windows.Input;
using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Client.Features.Tray;

/// <summary>Drives the tray icon and its menu.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Board 18 of the companion pairing mockup: a click on the icon or Open shows the window, Quit ends the
/// companion (owner decisions on #514).
/// </remarks>
public sealed class TrayViewModel
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="TrayViewModel"/> class.</summary>
    /// <param name="shell">The application shell that shows the window and ends the companion.</param>
    public TrayViewModel(IApplicationShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        OpenCommand = new RelayCommand(shell.ShowWindow);
        QuitCommand = new RelayCommand(shell.Quit);
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the command behind a click on the icon and the Open entry.</summary>
    public ICommand OpenCommand { get; }

    /// <summary>Gets the command behind the Quit entry.</summary>
    public ICommand QuitCommand { get; }
    #endregion Public Properties
}
