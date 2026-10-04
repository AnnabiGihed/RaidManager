namespace RaidManager.Companion.Client.Features.Shared;

/// <summary>Shows the companion's window and ends the companion.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the tray menu act on the window and the application without depending on the UI framework (board 18
/// of the companion pairing mockup).
/// </remarks>
public interface IApplicationShell
{
    #region Public Methods
    /// <summary>Shows the window, bringing it back from the tray or from the taskbar.</summary>
    void ShowWindow();

    /// <summary>Ends the companion.</summary>
    void Quit();
    #endregion Public Methods
}
