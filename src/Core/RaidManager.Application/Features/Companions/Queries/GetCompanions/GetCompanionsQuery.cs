using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace RaidManager.Application.Features.Companions.Queries.GetCompanions;

/// <summary>Lists a player's paired companions.</summary>
/// <param name="UserId">The signed-in player, from the website session.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Feeds the website's paired companions list, which shows revoked computers too.
/// </remarks>
public sealed record GetCompanionsQuery(Guid UserId) : IQuery<IReadOnlyList<CompanionResponse>>;
