namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Represents the content of <c>sync-queue.json</c>.</summary>
/// <param name="Queued">The snapshots waiting to upload, oldest first.</param>
/// <param name="Uploaded">The latest accepted snapshot of each character.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The queue's file shape, so nothing waiting is lost when the companion stops (#550).
/// </remarks>
internal sealed record SnapshotQueueFile(IReadOnlyList<QueuedSnapshot> Queued, IReadOnlyList<UploadedSnapshot> Uploaded);
