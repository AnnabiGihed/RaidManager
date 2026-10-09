using RaidManager.Companion.Client.Features.Sync.Queue;

namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Uploads character snapshots to RaidManager.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Hides HTTP from the upload rules, so they are tested with a fake (#550).
/// </remarks>
internal interface ISnapshotApi
{
    #region Public Methods
    /// <summary>Uploads one snapshot with the device token.</summary>
    /// <param name="deviceToken">The device token.</param>
    /// <param name="snapshot">The snapshot.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>What RaidManager answered; a network failure or timeout is <see cref="SnapshotUploadOutcome.Unavailable"/>.</returns>
    Task<SnapshotUploadResult> UploadAsync(string deviceToken, QueuedSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>Asks RaidManager whether the player asked this companion to send every character again (#615).</summary>
    /// <param name="deviceToken">The companion's device token.</param>
    /// <param name="cancellationToken">A token to stop the call.</param>
    /// <returns>When the player asked, or <see langword="null"/> when they never did or RaidManager couldn't be asked.</returns>
    Task<DateTimeOffset?> GetSyncAgainRequestAsync(string deviceToken, CancellationToken cancellationToken);
    #endregion Public Methods
}
