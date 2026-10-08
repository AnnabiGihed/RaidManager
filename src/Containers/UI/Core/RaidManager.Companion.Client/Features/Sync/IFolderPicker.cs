namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Asks the player to choose a folder.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Board 1's "Add a folder" opens Windows' folder picker (owner decision on #551); the host implements it with
/// the window's storage provider.
/// </remarks>
public interface IFolderPicker
{
    #region Public Methods
    /// <summary>Opens the folder picker.</summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>The chosen folder's path, or <see langword="null"/> when the player canceled.</returns>
    Task<string?> PickFolderAsync(CancellationToken cancellationToken);
    #endregion Public Methods
}
