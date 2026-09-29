using RaidManager.Domain.Features.Shared.Enums;

namespace RaidManager.Domain.Features.Raids.ValueObjects;

/// <summary>Represents one required instance and difficulty of a possibly combined raid.</summary>
/// <param name="Instance">The raid instance.</param>
/// <param name="Difficulty">The raid size and difficulty.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Lets a combined raid require several instances, each receiving its own readiness verdict at raid start.
/// </remarks>
public sealed record RaidTarget(RaidInstance Instance, RaidDifficulty Difficulty)
{
    #region Overrides
    /// <inheritdoc />
    public override string ToString() => $"{Instance} {Difficulty}";
    #endregion Overrides
}
