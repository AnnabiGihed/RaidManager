using RaidManager.Domain.Features.Characters.Enums;

namespace RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

/// <summary>Names a loadout in the My characters list.</summary>
/// <param name="Name">The loadout name, from the equipment set.</param>
/// <param name="Role">The raid role.</param>
/// <param name="GearScore">The calculated GearScore.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Gives board 1 its primary loadout column, for example Frost DPS with its GearScore.
/// </remarks>
public sealed record LoadoutSummaryResponse(string Name, CharacterRole Role, int GearScore);
