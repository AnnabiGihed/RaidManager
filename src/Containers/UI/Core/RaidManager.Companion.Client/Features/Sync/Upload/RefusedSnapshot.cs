using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Represents a snapshot RaidManager refused and the companion dropped.</summary>
/// <param name="Character">The character.</param>
/// <param name="ErrorCode">The problem's error code, such as <c>Character.Snapshot.IdentityUnavailable</c>, when given.</param>
/// <param name="RefusedAt">When it was refused.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: An actionable failure the sync state shows (owner decision on #550: a refused snapshot leaves the queue).
/// </remarks>
public sealed record RefusedSnapshot(CharacterKey Character, string? ErrorCode, DateTimeOffset RefusedAt);
