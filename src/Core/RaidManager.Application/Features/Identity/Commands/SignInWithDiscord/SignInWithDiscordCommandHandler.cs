using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Identity.Aggregates;
using RaidManager.Domain.Features.Identity.Repositories;
using RaidManager.Domain.Features.Identity.ValueObjects;

namespace RaidManager.Application.Features.Identity.Commands.SignInWithDiscord;

/// <summary>Handles <see cref="SignInWithDiscordCommand"/>: finds the user by Discord id or registers them, then commits.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-30<br/>
/// Purpose: Keeps one local user per Discord account; the unique index on the Discord id guards concurrent first sign-ins.
/// </remarks>
internal sealed class SignInWithDiscordCommandHandler : ICommandHandler<SignInWithDiscordCommand, Guid>
{
    #region Fields
    /// <summary>Stores the user repository.</summary>
    private readonly IUserRepository _users;

    /// <summary>Stores the unit of work that commits a registration or profile change.</summary>
    private readonly IUnitOfWork _unitOfWork;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="SignInWithDiscordCommandHandler"/> class.</summary>
    /// <param name="users">The user repository.</param>
    /// <param name="unitOfWork">The unit of work that commits a registration or profile change.</param>
    public SignInWithDiscordCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Resolves the Discord identity to its local user.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The local user identifier, or the commit failure with its <see cref="ResultExceptionType"/>.</returns>
    public async Task<Result<Guid>> Handle(SignInWithDiscordCommand request, CancellationToken cancellationToken)
    {
        var discordUserId = DiscordUserId.Create(request.DiscordUserId);
        var user = await _users.FindByDiscordIdAsync(discordUserId, cancellationToken);
        if (user is null)
        {
            user = User.Register(discordUserId, request.DisplayName, request.AvatarUrl);
            await _users.AddAsync(user, cancellationToken);
        }
        else if (user.UpdateDiscordProfile(request.DisplayName, request.AvatarUrl))
        {
            await _users.UpdateAsync(user, cancellationToken);
        }
        else
        {
            return user.Id.Value;
        }

        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? Result.Failure<Guid>(saved.Error, saved.ResultExceptionType) : user.Id.Value;
    }
    #endregion Public Methods
}
