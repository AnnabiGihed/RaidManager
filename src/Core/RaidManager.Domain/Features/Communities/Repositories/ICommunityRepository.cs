using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Communities.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Communities.Repositories;

/// <summary>Defines persistence operations for the raiding community aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer on Pivot's command repository, while implementations remain in EF Core infrastructure.
/// </remarks>
public interface ICommunityRepository : IAsyncCommandRepository<Community, CommunityId>;
