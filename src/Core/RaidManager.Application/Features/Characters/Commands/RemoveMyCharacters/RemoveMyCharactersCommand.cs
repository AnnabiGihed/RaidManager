using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Characters.Commands.RemoveMyCharacters;

/// <summary>Removes a player's characters, claims and loadouts so a sync or review test starts over.</summary>
/// <param name="UserId">The player whose characters are removed.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-09<br/>
/// Purpose: The dev and test reset of story #597; the API offers it only on Development, Dev and Test. The result is
/// the number of characters the player no longer has.
/// </remarks>
public sealed record RemoveMyCharactersCommand(Guid UserId) : ICommand<int>;
