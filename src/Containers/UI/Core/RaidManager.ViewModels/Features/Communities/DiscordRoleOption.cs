namespace RaidManager.ViewModels.Features.Communities;

/// <summary>Describes a Discord role the Administrator can map.</summary>
/// <param name="Id">The role snowflake.</param>
/// <param name="Name">The role name.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: One option of the roles card's role picker.
/// </remarks>
public sealed record DiscordRoleOption(string Id, string Name);
