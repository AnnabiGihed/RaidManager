using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanionPairing;

/// <summary>Gets the pairing a code belongs to, so the player can check it before confirming.</summary>
/// <param name="UserId">The signed-in player asking, from the website session.</param>
/// <param name="PairingCode">The code, with or without its dash.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Feeds the website's confirmation page: the code, the computer's label, the request time and the expiry.
/// </remarks>
public sealed record GetCompanionPairingQuery(Guid UserId, string PairingCode) : IQuery<CompanionPairingResponse>;
