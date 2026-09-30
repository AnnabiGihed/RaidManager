using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Characters.Repositories;

/// <summary>Loads and tracks <see cref="Character"/> aggregates in the RaidManager database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Implements the domain repository contract on Pivot's command repository; the owned claims, loadouts and raid saves load with the character.
/// </remarks>
internal sealed class CharacterRepository : BaseAsyncCommandRepository<Character, CharacterId>, ICharacterRepository
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterRepository"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public CharacterRepository(RaidManagerDbContext dbContext)
        : base(dbContext)
    {
    }
    #endregion Constructors
}
