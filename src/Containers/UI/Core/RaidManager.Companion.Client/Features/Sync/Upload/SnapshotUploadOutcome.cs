namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Defines how RaidManager answered one snapshot upload.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Maps the answers of <c>POST /companion/snapshots</c> (#384) to what the queue does next (#550).
/// </remarks>
internal enum SnapshotUploadOutcome
{
    /// <summary>202 <c>Imported</c> or 200 <c>AlreadyCurrent</c>: the snapshot leaves the queue.</summary>
    Accepted,

    /// <summary>400: the snapshot breaks the contract; it leaves the queue as a failure (owner decision on #550).</summary>
    Refused,

    /// <summary>401: RaidManager refuses the device token; uploads stop until the companion pairs again.</summary>
    Unauthorized,

    /// <summary>429: the companion uploads too fast and waits as long as RaidManager asks.</summary>
    SlowDown,

    /// <summary>A network failure, a timeout or a server error: retried with growing waits.</summary>
    Unavailable,
}
