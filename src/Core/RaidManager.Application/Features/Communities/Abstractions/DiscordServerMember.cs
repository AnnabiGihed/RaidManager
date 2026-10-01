namespace RaidManager.Application.Features.Communities.Abstractions;

/// <summary>Describes a person in a Discord server, as Discord reports them.</summary>
/// <param name="UserId">The Discord user snowflake.</param>
/// <param name="DisplayName">The name shown in the server: the server nickname, else the global name, else the username.</param>
/// <param name="RoleIds">The member's role snowflakes in the server.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets RaidManager count and list a community's members from Discord without storing them (ADR-0022).
/// </remarks>
public sealed record DiscordServerMember(string UserId, string DisplayName, IReadOnlyList<string> RoleIds);
