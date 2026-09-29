using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Characters.Repositories;

/// <summary>Defines persistence operations for the Warmane character aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer on Pivot's command repository, while implementations remain in EF Core infrastructure.
/// </remarks>
public interface ICharacterRepository : IAsyncCommandRepository<Character, CharacterId>;
