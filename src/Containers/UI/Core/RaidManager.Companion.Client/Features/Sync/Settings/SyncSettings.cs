using System.Text.Json.Serialization;

namespace RaidManager.Companion.Client.Features.Sync.Settings;

/// <summary>Represents the player's sync choices, kept in <c>sync-settings.json</c>.</summary>
/// <param name="FoundFolders">The installations the last search found, or <see langword="null"/> before the first search.</param>
/// <param name="AddedFolders">The installations the player chose with Choose folders.</param>
/// <param name="ExcludedAccounts">The hashes of the accounts the player cleared on board 1.</param>
/// <param name="Paused">Whether the player paused sync.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Keeps the watched folders, the exclusions and the pause across restarts (#550). Accounts are kept as
/// hashes: the folder name is the player's WoW login (avalonia-desktop §1).
/// </remarks>
internal sealed record SyncSettings(
    IReadOnlyList<string>? FoundFolders,
    IReadOnlyList<string> AddedFolders,
    IReadOnlyList<string> ExcludedAccounts,
    bool Paused)
{
    #region Public Properties
    /// <summary>Gets the settings of a companion that hasn't searched yet.</summary>
    public static SyncSettings Initial { get; } = new(null, [], [], false);

    /// <summary>Gets every watched installation folder, found or added, without duplicates.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> Folders =>
        [.. (FoundFolders ?? []).Concat(AddedFolders).Distinct(StringComparer.OrdinalIgnoreCase)];
    #endregion Public Properties
}
