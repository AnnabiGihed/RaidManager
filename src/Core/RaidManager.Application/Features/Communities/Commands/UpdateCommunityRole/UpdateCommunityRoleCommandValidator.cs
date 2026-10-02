using FluentValidation;
using RaidManager.Application.Features.Communities.Abstractions;
using RaidManager.Domain.Features.Communities.Aggregates;

namespace RaidManager.Application.Features.Communities.Commands.UpdateCommunityRole;

/// <summary>Validates the shape of an <see cref="UpdateCommunityRoleCommand"/> before it reaches its handler.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: Rejects missing identifiers, a blank or too long name, and unknown permission names at the boundary.
/// </remarks>
public sealed class UpdateCommunityRoleCommandValidator : AbstractValidator<UpdateCommunityRoleCommand>
{
    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="UpdateCommunityRoleCommandValidator"/> class.</summary>
    public UpdateCommunityRoleCommandValidator()
    {
        RuleFor(command => command.CommunityId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CommunityRole.MaximumNameLength);
        RuleFor(command => command.Permissions).NotNull();
        RuleForEach(command => command.Permissions)
            .Must(CommunityPermissionNames.IsKnown)
            .WithMessage("A permission is one of ManageRaids, BuildRosters, RunRaidNight, ReviewConflicts or ManageCommunityRoles.");
    }
    #endregion Constructors
}
