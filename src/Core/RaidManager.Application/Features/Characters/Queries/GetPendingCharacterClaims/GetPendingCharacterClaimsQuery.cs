using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Characters.Queries.GetPendingCharacterClaims;

/// <summary>Requests the characters on which a player has a claim awaiting their decision.</summary>
/// <param name="UserId">The player whose claims are listed.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Feeds the review page a player sees at sign-in: newly discovered characters and ownership conflicts.
/// </remarks>
public sealed record GetPendingCharacterClaimsQuery(Guid UserId) : IQuery<IReadOnlyList<PendingCharacterClaimResponse>>;
