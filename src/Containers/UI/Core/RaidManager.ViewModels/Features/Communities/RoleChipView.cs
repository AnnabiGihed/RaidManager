namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a mapped Discord role as a chip.</summary>
/// <param name="DiscordRoleId">The role snowflake.</param>
/// <param name="Text">The chip text: "@" and the role name, or "Deleted role".</param>
/// <param name="Missing">Whether the role was deleted in Discord.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One chip of the roles card of boards 4 and 8.
/// </remarks>
public sealed record RoleChipView(string DiscordRoleId, string Text, bool Missing);
