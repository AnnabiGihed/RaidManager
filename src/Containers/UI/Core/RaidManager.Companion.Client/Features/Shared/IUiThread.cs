namespace RaidManager.Companion.Client.Features.Shared;

/// <summary>Runs work on the window's thread.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Brings a change that arrives on a background thread, such as the sync's status (#550), to the thread the
/// views read their view models on, without the client knowing the UI framework (avalonia-desktop §8).
/// </remarks>
public interface IUiThread
{
    #region Public Methods
    /// <summary>Queues work to run on the window's thread.</summary>
    /// <param name="action">The work.</param>
    void Post(Action action);
    #endregion Public Methods
}
