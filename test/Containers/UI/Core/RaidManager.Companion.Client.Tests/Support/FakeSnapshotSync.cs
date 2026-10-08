using RaidManager.Companion.Client.Features.Sync;

namespace RaidManager.Companion.Client.Tests.Support;

/// <summary>Stands in for the background sync: a status the test sets, and a record of the player's actions.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-08<br/>
/// Purpose: Lets the sync screens' tests put the sync in any state of the boards and check what the screens asked of
/// it, without files, a clock or a network (#551).
/// </remarks>
internal sealed class FakeSnapshotSync : ISnapshotSync
{
    #region Fields
    /// <summary>Stores the status.</summary>
    private SyncStatus _status = SyncStatuses.Watching();
    #endregion Fields

    #region Events
    /// <inheritdoc />
    public event EventHandler? StatusChanged;
    #endregion Events

    #region Public Properties
    /// <inheritdoc />
    public SyncStatus Status => _status;

    /// <summary>Gets the actions the screens asked for, in order, such as <c>pause</c> or <c>exclude ALTACC-id</c>.</summary>
    public List<string> Actions { get; } = [];

    /// <summary>Gets or sets a value indicating whether a folder added is a WoW installation.</summary>
    public bool AcceptsFolders { get; set; } = true;

    /// <summary>Gets or sets the failure every action throws, if any.</summary>
    public Exception? Failure { get; set; }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Changes the status and raises <see cref="StatusChanged"/>, as the sync does from its thread.</summary>
    /// <param name="status">The new status.</param>
    public void Change(SyncStatus status)
    {
        _status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Gets the number of handlers listening to the status.</summary>
    /// <returns>The number of handlers.</returns>
    public int ListenerCount() => StatusChanged?.GetInvocationList().Length ?? 0;

    /// <inheritdoc />
    public Task PauseAsync(CancellationToken cancellationToken) => Record("pause");

    /// <inheritdoc />
    public Task ResumeAsync(CancellationToken cancellationToken) => Record("resume");

    /// <inheritdoc />
    public void RetryNow() => Actions.Add("retry now");

    /// <inheritdoc />
    public Task ReadAgainAsync(CancellationToken cancellationToken) => Record("read again");

    /// <inheritdoc />
    public Task FindFoldersAgainAsync(CancellationToken cancellationToken) => Record("find folders again");

    /// <inheritdoc />
    public async Task<bool> AddFolderAsync(string folder, CancellationToken cancellationToken)
    {
        await Record($"add {folder}");
        return AcceptsFolders;
    }

    /// <inheritdoc />
    public Task SetAccountWatchedAsync(string accountId, bool watched, CancellationToken cancellationToken) =>
        Record(watched ? $"watch {accountId}" : $"exclude {accountId}");

    /// <inheritdoc />
    public Task SetFolderWatchedAsync(string folder, bool watched, CancellationToken cancellationToken) =>
        Record(watched ? $"watch folder {folder}" : $"exclude folder {folder}");
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Records an action, or fails it.</summary>
    /// <param name="action">The action.</param>
    /// <returns>A completed or failed task.</returns>
    private Task Record(string action)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        Actions.Add(action);
        return Task.CompletedTask;
    }
    #endregion Private Helpers
}
