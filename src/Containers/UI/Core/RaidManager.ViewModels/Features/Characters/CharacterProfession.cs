namespace RaidManager.ViewModels.Features.Characters;

/// <summary>Describes a synchronized profession on a profile.</summary>
/// <param name="Name">The profession name.</param>
/// <param name="Rank">The current skill.</param>
/// <param name="MaxRank">The maximum skill of the current training.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Carries one row of the profile's Professions card.
/// </remarks>
public sealed record CharacterProfession(string Name, int Rank, int MaxRank);
