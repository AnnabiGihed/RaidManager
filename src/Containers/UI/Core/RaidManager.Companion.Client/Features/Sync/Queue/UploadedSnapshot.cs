namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Records the latest snapshot RaidManager accepted for a character.</summary>
/// <param name="Realm">The character's realm.</param>
/// <param name="Name">The character's name.</param>
/// <param name="CapturedAt">The capture time of the latest snapshot the API answered 202 or 200 to.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Keeps the companion from queuing the same snapshot again each time it reads an unchanged file (#550).
/// </remarks>
internal sealed record UploadedSnapshot(string Realm, string Name, long CapturedAt);
