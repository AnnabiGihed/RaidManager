using RaidManager.Companion.Client.Features.Shared;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Stands in for the window's thread: work waits until the test runs it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Shows that the sync screens change only on the window's thread, never on the thread that raised the status
/// (#551).
/// </remarks>
internal sealed class QueuedUiThread : IUiThread
{
    #region Fields
    /// <summary>Stores the work waiting.</summary>
    private readonly Queue<Action> _queue = new();
    #endregion Fields

    #region Public Properties
    /// <summary>Gets the number of works waiting.</summary>
    public int Pending => _queue.Count;
    #endregion Public Properties

    #region Public Methods
    /// <inheritdoc />
    public void Post(Action action) => _queue.Enqueue(action);

    /// <summary>Runs every work waiting, in order.</summary>
    public void RunAll()
    {
        while (_queue.TryDequeue(out var action))
        {
            action();
        }
    }
    #endregion Public Methods
}
