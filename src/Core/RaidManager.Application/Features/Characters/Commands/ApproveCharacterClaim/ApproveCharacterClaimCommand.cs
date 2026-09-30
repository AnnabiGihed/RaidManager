using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace RaidManager.Application.Features.Characters.Commands.ApproveCharacterClaim;

/// <summary>Requests that a player approves their pending claim on a character.</summary>
/// <param name="CharacterId">The claimed character.</param>
/// <param name="UserId">The player deciding on their own claim.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Carries the player's explicit decision on a discovered character; the decision makes the player the character's owner, or moves the claim to conflict review when another player already owns it.
/// </remarks>
public sealed record ApproveCharacterClaimCommand(Guid CharacterId, Guid UserId) : ICommand;
