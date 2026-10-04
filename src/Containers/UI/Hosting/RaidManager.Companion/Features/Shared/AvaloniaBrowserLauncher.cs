using Avalonia.Controls;
using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Features.Shared;

/// <summary>Opens an address in the default browser through the window's launcher.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Implements the browser launcher with Avalonia's <c>Launcher</c>, which opens the default browser on
/// Windows (avalonia-desktop §4).
/// </remarks>
internal sealed class AvaloniaBrowserLauncher : IBrowserLauncher
{
    #region Fields
    /// <summary>Stores the shell holding the window whose launcher is used.</summary>
    private readonly ApplicationShell _shell;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="AvaloniaBrowserLauncher"/> class.</summary>
    /// <param name="shell">The shell holding the companion's window.</param>
    public AvaloniaBrowserLauncher(ApplicationShell shell)
    {
        _shell = shell;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public async Task<bool> OpenAsync(Uri address)
    {
        var launcher = _shell.Window is { } window ? TopLevel.GetTopLevel(window)?.Launcher : null;
        return launcher is not null && await launcher.LaunchUriAsync(address);
    }
    #endregion Public Methods
}
