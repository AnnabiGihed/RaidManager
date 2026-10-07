namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Represents RaidManager's answer to one snapshot upload.</summary>
/// <param name="Outcome">What the answer means for the queue.</param>
/// <param name="ErrorCode">The problem's error code of a refusal, such as <c>Character.Snapshot.IdentityUnavailable</c>.</param>
/// <param name="RetryAfter">How long RaidManager asks to wait after a 429, when it says.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The result of <see cref="ISnapshotApi.UploadAsync"/> (#550).
/// </remarks>
internal sealed record SnapshotUploadResult(SnapshotUploadOutcome Outcome, string? ErrorCode = null, TimeSpan? RetryAfter = null);
