using System.Text.Json.Nodes;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Queue;

/// <summary>Represents one character snapshot waiting to upload.</summary>
/// <param name="AccountId">The hashed account folder it came from, so excluding the account can drop it.</param>
/// <param name="Realm">The character's realm.</param>
/// <param name="Name">The character's name.</param>
/// <param name="CapturedAt">When the addon wrote it, in seconds since 1970 (UTC).</param>
/// <param name="AddonVersion">The addon version that wrote the file.</param>
/// <param name="Character">The character's entry, as the addon wrote it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The unit of the upload queue (#550). Realm, name and capture time identify it, so a retry sends the same
/// snapshot and the API answers <c>AlreadyCurrent</c> to one it already applied (#384). The account is kept as a hash:
/// the folder name is the player's WoW login and never reaches a file but <c>companion.dat</c> (avalonia-desktop §1).
/// </remarks>
internal sealed record QueuedSnapshot(string AccountId, string Realm, string Name, long CapturedAt, string? AddonVersion, JsonObject Character)
{
    #region Public Properties
    /// <summary>Gets the character the snapshot belongs to.</summary>
    public CharacterKey Key => new(Realm, Name);
    #endregion Public Properties
}
