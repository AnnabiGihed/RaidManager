using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.ValueObjects;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Tests.Features.Raids.Support;

/// <summary>Builds raid-start readiness assessments for scenarios that are not about readiness itself.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Lets signup and roster scenarios supply a current assessment without synchronizing a full character.
/// </remarks>
public static class ReadinessAssessments
{
    #region Public Methods
    /// <summary>Creates an assessment in which the character is available for every target of the raid.</summary>
    /// <param name="raid">The raid whose targets and start apply.</param>
    /// <param name="characterId">The assessed character.</param>
    /// <returns>A current, available readiness.</returns>
    public static CharacterReadiness Available(Raid raid, CharacterId characterId) =>
        CharacterReadiness.Create(
            characterId,
            raid.StartsAtUtc,
            raid.Targets.Select(target => new EligibilityAssessment(target, ReadinessVerdict.Available, null)));

    /// <summary>Creates available assessments for every character offered in the options.</summary>
    /// <param name="raid">The raid whose targets and start apply.</param>
    /// <param name="options">The offered options.</param>
    /// <returns>One current, available readiness per offered character.</returns>
    public static CharacterReadiness[] AvailableFor(Raid raid, IEnumerable<SignupOption> options) =>
        [.. options.Select(option => Available(raid, option.CharacterId))];
    #endregion Public Methods
}
