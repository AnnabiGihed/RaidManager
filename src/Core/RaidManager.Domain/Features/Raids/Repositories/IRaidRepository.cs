using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.Repositories;

/// <summary>Defines persistence operations for the raid aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer on Pivot's command repository, while implementations remain in EF Core infrastructure.
/// </remarks>
public interface IRaidRepository : IAsyncCommandRepository<Raid, RaidId>;
