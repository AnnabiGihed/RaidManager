using System.Text.Json.Serialization;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Records the latest snapshot RaidManager accepted for a character.</summary>
/// <param name="Realm">The character's realm.</param>
/// <param name="Name">The character's name.</param>
/// <param name="CapturedAt">The capture time of the latest snapshot the API answered 202 or 200 to.</param>
/// <param name="UploadedAt">When RaidManager accepted it, or <see langword="null"/> in a file written before #592.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Keeps the companion from queuing the same snapshot again each time it reads an unchanged file (#550), and
/// gives the sync screen its last success and recent uploads after a restart (#592).
/// </remarks>
internal sealed record UploadedSnapshot(string Realm, string Name, long CapturedAt, DateTimeOffset? UploadedAt = null)
{
    #region Public Properties
    /// <summary>Gets the character the snapshot belongs to.</summary>
    [JsonIgnore]
    public CharacterKey Character => new(Realm, Name);
    #endregion Public Properties
}
