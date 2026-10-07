using RaidManager.Companion.Client.Features.Sync.SavedVariables;

namespace RaidManager.Companion.Client.Features.Sync.Upload;

/// <summary>Represents a character whose snapshot RaidManager accepted.</summary>
/// <param name="Character">The character.</param>
/// <param name="UploadedAt">When RaidManager accepted it.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: An "Uploaded today, 14:05" row of board 2's recent activity (#550).
/// </remarks>
public sealed record UploadedCharacter(CharacterKey Character, DateTimeOffset UploadedAt);
