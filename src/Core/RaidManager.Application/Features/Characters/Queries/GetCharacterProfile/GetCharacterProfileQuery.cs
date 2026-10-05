using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Characters.Queries.GetCharacterProfile;

/// <summary>Requests the profile of one of the player's characters.</summary>
/// <param name="UserId">The player asking.</param>
/// <param name="CharacterId">The character whose profile is shown.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Feeds board 2 of the character profile mockup (story #19). Anyone but the character's verified owner gets not found (owner decision on #19, 2026-10-05).
/// </remarks>
public sealed record GetCharacterProfileQuery(Guid UserId, Guid CharacterId) : IQuery<CharacterProfileResponse>;
