using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Persistence.EntityFrameworkCore.Features.Identity.Repositories;

/// <summary>Loads and tracks <see cref="User"/> aggregates in the RaidManager database.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Implements the domain repository contract on Pivot's command repository, adding the lookup by Discord account.
/// </remarks>
internal sealed class UserRepository : BaseAsyncCommandRepository<User, UserId>, IUserRepository
{
    #region Fields
    /// <summary>Stores the RaidManager database context.</summary>
    private readonly RaidManagerDbContext _dbContext;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="UserRepository"/> class.</summary>
    /// <param name="dbContext">The RaidManager database context.</param>
    public UserRepository(RaidManagerDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public Task<User?> FindByDiscordIdAsync(DiscordUserId discordUserId, CancellationToken cancellationToken) =>
        _dbContext.Users.FirstOrDefaultAsync(user => user.DiscordUserId == discordUserId, cancellationToken);
    #endregion Public Methods
}
