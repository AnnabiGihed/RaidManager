namespace RaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents the guild the addon saw the character in.</summary>
/// <param name="Name">The guild name, or <see langword="null"/> when the character is in no guild.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Separates an observed absence of guild from a guild the game didn't report (#384).
/// </remarks>
public sealed record AddonGuild(string? Name);
