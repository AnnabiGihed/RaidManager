using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Characters.Commands.RejectCharacterClaim;

/// <summary>Requests that a player rejects their pending claim on a character.</summary>
/// <param name="CharacterId">The claimed character.</param>
/// <param name="UserId">The player deciding on their own claim.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Carries the player's explicit decision on a discovered character; the decision keeps the character out of the player's raid signup options.
/// </remarks>
public sealed record RejectCharacterClaimCommand(Guid CharacterId, Guid UserId) : ICommand;
