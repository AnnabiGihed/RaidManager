using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Features.Shared;

/// <summary>Shows the companion's window and ends the companion, for the tray menu and a second start.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the application shell over Avalonia's desktop lifetime (board 18 of the companion pairing
/// mockup); the window is attached once the composition root created it.
/// </remarks>
internal sealed class ApplicationShell : IApplicationShell
{
    #region Fields
    /// <summary>Stores the desktop lifetime that ends the companion.</summary>
    private readonly IClassicDesktopStyleApplicationLifetime? _lifetime;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ApplicationShell"/> class.</summary>
    /// <param name="lifetime">The desktop lifetime, or <see langword="null"/> where there is none.</param>
    public ApplicationShell(IClassicDesktopStyleApplicationLifetime? lifetime)
    {
        _lifetime = lifetime;
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the companion's window, once attached.</summary>
    public Window? Window { get; private set; }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Attaches the companion's window.</summary>
    /// <param name="window">The window.</param>
    public void Attach(Window window) => Window = window;

    /// <inheritdoc />
    public void ShowWindow()
    {
        if (Window is null)
        {
            return;
        }

        Window.Show();
        if (Window.WindowState == WindowState.Minimized)
        {
            Window.WindowState = WindowState.Normal;
        }

        Window.Activate();
    }

    /// <inheritdoc />
    public void Quit() => _lifetime?.Shutdown();
    #endregion Public Methods
}
