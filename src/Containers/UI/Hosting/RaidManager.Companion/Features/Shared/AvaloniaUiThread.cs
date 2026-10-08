using Avalonia.Threading;
using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Features.Shared;

/// <summary>Runs work on Avalonia's UI thread.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Brings the sync's status, raised on a background thread, to the window's thread (#551,
/// avalonia-desktop §8).
/// </remarks>
internal sealed class AvaloniaUiThread : IUiThread
{
    #region Public Methods
    /// <inheritdoc />
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
    #endregion Public Methods
}
