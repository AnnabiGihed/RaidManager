namespace RaidManager.Companion.Client.Features.Shared;

/// <summary>Opens an address in the player's default browser.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Keeps the view models free of UI framework types; the host implements it with the window's launcher.
/// </remarks>
public interface IBrowserLauncher
{
    #region Public Methods
    /// <summary>Opens an address in the default browser.</summary>
    /// <param name="address">The address.</param>
    /// <returns><see langword="true"/> when the system accepted the request.</returns>
    Task<bool> OpenAsync(Uri address);
    #endregion Public Methods
}
