using FluentValidation;

namespace RaidManager.Application.Features.Communities.Commands.DeleteCommunityRole;

/// <summary>Validates the shape of a <see cref="DeleteCommunityRoleCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Rejects missing identifiers at the boundary.
/// </remarks>
public sealed class DeleteCommunityRoleCommandValidator : AbstractValidator<DeleteCommunityRoleCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DeleteCommunityRoleCommandValidator"/> class.</summary>
    public DeleteCommunityRoleCommandValidator()
    {
        RuleFor(command => command.CommunityId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RoleId).NotEmpty();
    }
    #endregion Constructors
}
