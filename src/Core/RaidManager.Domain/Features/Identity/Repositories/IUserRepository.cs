using Pivot.Framework.Domain.Repositories;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Identity.Repositories;

/// <summary>Defines persistence operations for the application user aggregate root.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps aggregate persistence contracts in the Domain layer on Pivot's command repository, while implementations remain in EF Core infrastructure.
/// </remarks>
public interface IUserRepository : IAsyncCommandRepository<User, UserId>
{
    #region Methods
    /// <summary>Finds the user linked to a Discord account.</summary>
    /// <param name="discordUserId">The Discord account identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The linked user, or <see langword="null"/> when the Discord account has never signed in.</returns>
    Task<User?> FindByDiscordIdAsync(DiscordUserId discordUserId, CancellationToken cancellationToken);
    #endregion Methods
}
