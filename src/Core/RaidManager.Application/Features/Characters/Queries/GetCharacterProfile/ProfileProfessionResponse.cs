namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Describes a synchronized profession on a profile.</summary>
/// <param name="Name">The profession name.</param>
/// <param name="Rank">The current skill.</param>
/// <param name="MaxRank">The maximum skill of the current training.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds the profile's Professions card with the facts the addon read.
/// </remarks>
public sealed record ProfileProfessionResponse(string Name, int Rank, int MaxRank);
