using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Raids.Aggregates;
using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Raids.ValueObjects;

namespace RaidManager.Domain.Features.Raids.Services;

/// <summary>Evaluates a character against every required target of a raid at the scheduled raid start.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps the raid-start readiness rules in one pure domain calculation shared by the website and Discord signup paths.
/// Validated realm reset schedules and difficulty sharing are not applied yet; they follow the Warmane rule matrix (#40).
/// </remarks>
public static class RaidReadinessEvaluator
{
    #region Public Methods
    /// <summary>Assesses a character for every target of a raid.</summary>
    /// <param name="character">The approved character to assess.</param>
    /// <param name="raid">The raid whose targets and scheduled start apply.</param>
    /// <param name="nowUtc">The UTC instant of the assessment, used to judge evidence freshness.</param>
    /// <returns>The per-target verdicts and their most restrictive combination.</returns>
    public static CharacterReadiness Assess(Character character, Raid raid, DateTimeOffset nowUtc)
    {
        var hasFreshEvidence = HasFreshLockoutEvidence(character, raid.Requirements.MaximumCharacterDataAge, nowUtc);
        var assessments = raid.Targets.Select(target => AssessTarget(character, target, raid.StartsAtUtc, hasFreshEvidence));
        return CharacterReadiness.Create(character.Id, raid.StartsAtUtc, assessments);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Assesses one required target.</summary>
    /// <param name="character">The assessed character.</param>
    /// <param name="target">The required instance and difficulty.</param>
    /// <param name="raidStartsAtUtc">The scheduled raid start.</param>
    /// <param name="hasFreshEvidence">Whether the character's lockout snapshot is recent enough to trust.</param>
    /// <returns>The target's verdict, with the matching save's reset time when one is known.</returns>
    private static EligibilityAssessment AssessTarget(Character character, RaidTarget target, DateTimeOffset raidStartsAtUtc, bool hasFreshEvidence)
    {
        var lockout = character.RaidLockouts
            .Where(candidate => candidate.Instance == target.Instance && candidate.Difficulty == target.Difficulty)
            .MaxBy(candidate => candidate.ResetsAtUtc);
        return new EligibilityAssessment(target, DecideVerdict(lockout, raidStartsAtUtc, hasFreshEvidence), lockout?.ResetsAtUtc);
    }

    /// <summary>Decides the verdict for one target from its matching save.</summary>
    /// <param name="lockout">The matching save, or <see langword="null"/> when the snapshot holds none.</param>
    /// <param name="raidStartsAtUtc">The scheduled raid start.</param>
    /// <param name="hasFreshEvidence">Whether the character's lockout snapshot is recent enough to trust.</param>
    /// <returns>The readiness verdict.</returns>
    private static ReadinessVerdict DecideVerdict(RaidLockout? lockout, DateTimeOffset raidStartsAtUtc, bool hasFreshEvidence)
    {
        if (!hasFreshEvidence)
        {
            return ReadinessVerdict.NeedsFreshSync;
        }

        if (lockout is null)
        {
            return ReadinessVerdict.Available;
        }

        if (lockout.ResetsAtUtc > raidStartsAtUtc)
        {
            return ReadinessVerdict.LockedThroughRaid;
        }

        // An extended save can outlast its observed reset, so it never counts as expiring before the raid.
        return lockout.IsExtended ? ReadinessVerdict.NeedsFreshSync : ReadinessVerdict.ResetsBeforeRaid;
    }

    /// <summary>Determines whether the character's addon lockout snapshot exists and is recent enough to trust.</summary>
    /// <param name="character">The assessed character.</param>
    /// <param name="maximumDataAge">The raid's maximum accepted evidence age.</param>
    /// <param name="nowUtc">The UTC instant of the assessment.</param>
    /// <returns><see langword="true"/> when the latest addon snapshot is within the accepted age.</returns>
    private static bool HasFreshLockoutEvidence(Character character, TimeSpan maximumDataAge, DateTimeOffset nowUtc) =>
        character.LastAddonSynchronizedAtUtc is { } observedAtUtc && nowUtc - observedAtUtc <= maximumDataAge;
    #endregion Private Helpers
}
