namespace RaidManager.Companion.Client.Features.Sync;

/// <summary>Reads, queues and uploads character snapshots in the background, and takes the player's sync choices.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: What #551's screens bind to (#550): the status, and the actions of boards 1 to 5 of <c>companion-sync</c>
/// (Save and sync, Find folders again, Choose folders, Pause, Resume, Retry now, Retry a character).
/// <see cref="StatusChanged"/> is raised on a background thread.
/// </remarks>
public interface ISnapshotSync
{
    #region Events
    /// <summary>Occurs when the status changed, on a background thread.</summary>
    event EventHandler? StatusChanged;
    #endregion Events

    #region Public Properties
    /// <summary>Gets the current status.</summary>
    SyncStatus Status { get; }
    #endregion Public Properties

    #region Public Methods
    /// <summary>Stops uploading; reading and queuing go on (owner decision on #550).</summary>
    /// <param name="cancellationToken">A token to cancel the change.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    Task PauseAsync(CancellationToken cancellationToken);

    /// <summary>Resumes uploading and uploads the queue at once.</summary>
    /// <param name="cancellationToken">A token to cancel the change.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    Task ResumeAsync(CancellationToken cancellationToken);

    /// <summary>Uploads at once, whatever the wait after a failure (board 4's "Retry now").</summary>
    void RetryNow();

    /// <summary>Reads every watched file again at once and forgets the refused snapshots (the retry of boards 5 to 8).</summary>
    /// <param name="cancellationToken">A token to cancel the change.</param>
    /// <returns>A task that completes when the next read is scheduled.</returns>
    Task ReadAgainAsync(CancellationToken cancellationToken);

    /// <summary>Searches the usual places of every fixed drive again (board 1's "Find folders again").</summary>
    /// <param name="cancellationToken">A token to stop the search.</param>
    /// <returns>A task that completes when the search is done.</returns>
    Task FindFoldersAgainAsync(CancellationToken cancellationToken);

    /// <summary>Watches a folder the player chose.</summary>
    /// <param name="folder">The folder, the one holding <c>WTF\Account</c>.</param>
    /// <param name="cancellationToken">A token to cancel the change.</param>
    /// <returns><see langword="true"/> when the folder is a WoW installation and is now watched.</returns>
    Task<bool> AddFolderAsync(string folder, CancellationToken cancellationToken);

    /// <summary>Watches or excludes an account; excluding drops its waiting snapshots (owner decision on #550).</summary>
    /// <param name="accountId">The account's hash.</param>
    /// <param name="watched">Whether to watch it.</param>
    /// <param name="cancellationToken">A token to cancel the change.</param>
    /// <returns>A task that completes when the choice is saved.</returns>
    Task SetAccountWatchedAsync(string accountId, bool watched, CancellationToken cancellationToken);
    #endregion Public Methods
}
