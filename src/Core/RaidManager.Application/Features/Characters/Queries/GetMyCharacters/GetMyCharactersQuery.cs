using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Characters.Queries.GetMyCharacters;

/// <summary>Requests the characters a player owns, for the My characters page.</summary>
/// <param name="UserId">The player whose characters are listed.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds board 1 of the character profile mockup (story #19): realm, class, level, primary loadout, raid saves and sync freshness.
/// </remarks>
public sealed record GetMyCharactersQuery(Guid UserId) : IQuery<IReadOnlyList<CharacterSummaryResponse>>;
