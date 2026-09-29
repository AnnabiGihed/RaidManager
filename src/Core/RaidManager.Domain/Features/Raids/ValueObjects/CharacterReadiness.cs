using RaidManager.Domain.Features.Raids.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Domain.Features.Raids.ValueObjects;

/// <summary>Represents the readiness of one character for every required target of a raid at its scheduled start.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Combines per-target verdicts into the most restrictive result that signup and roster rules enforce.
/// </remarks>
public sealed class CharacterReadiness
{
    #region Fields
    /// <summary>Stores one assessment per required raid target.</summary>
    private readonly List<EligibilityAssessment> _assessments;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CharacterReadiness"/> class.</summary>
    /// <param name="characterId">The assessed character.</param>
    /// <param name="evaluatedForUtc">The scheduled raid start the assessment was made for.</param>
    /// <param name="assessments">The per-target assessments.</param>
    private CharacterReadiness(CharacterId characterId, DateTimeOffset evaluatedForUtc, List<EligibilityAssessment> assessments)
    {
        CharacterId = characterId;
        EvaluatedForUtc = evaluatedForUtc;
        _assessments = assessments;
        Verdict = assessments.Max(assessment => assessment.Verdict);
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the assessed character.</summary>
    public CharacterId CharacterId { get; }

    /// <summary>Gets the scheduled raid start the assessment was made for.</summary>
    public DateTimeOffset EvaluatedForUtc { get; }

    /// <summary>Gets one assessment per required raid target.</summary>
    public IReadOnlyCollection<EligibilityAssessment> Assessments => _assessments.AsReadOnly();

    /// <summary>Gets the overall verdict: the most restrictive verdict of all targets.</summary>
    public ReadinessVerdict Verdict { get; }

    /// <summary>Gets a value indicating whether a confirmed active save blocks signup and roster assignment.</summary>
    public bool IsBlocked => Verdict == ReadinessVerdict.LockedThroughRaid;
    #endregion Properties

    #region Factory Methods
    /// <summary>Creates the readiness of a character from its per-target assessments.</summary>
    /// <param name="characterId">The assessed character.</param>
    /// <param name="evaluatedForUtc">The scheduled raid start the assessment was made for.</param>
    /// <param name="assessments">One assessment per required raid target.</param>
    /// <returns>The combined character readiness.</returns>
    /// <exception cref="DomainException">Thrown when no target is assessed or a target is assessed twice.</exception>
    public static CharacterReadiness Create(CharacterId characterId, DateTimeOffset evaluatedForUtc, IEnumerable<EligibilityAssessment> assessments)
    {
        var assessmentList = assessments.ToList();
        if (assessmentList.Count == 0)
        {
            throw new UnknownDomainException("A readiness assessment must cover at least one raid target.");
        }

        if (assessmentList.DistinctBy(assessment => assessment.Target).Count() != assessmentList.Count)
        {
            throw new UnknownDomainException("A readiness assessment cannot assess the same raid target twice.");
        }

        return new CharacterReadiness(characterId, evaluatedForUtc, assessmentList);
    }
    #endregion Factory Methods

    #region Domain Behavior
    /// <summary>Determines whether the assessment matches a raid's current targets and scheduled start.</summary>
    /// <param name="targets">The raid's required targets.</param>
    /// <param name="startsAtUtc">The raid's scheduled start.</param>
    /// <returns><see langword="true"/> when every current target, and only those, was assessed for that start.</returns>
    public bool IsCurrentFor(IReadOnlyCollection<RaidTarget> targets, DateTimeOffset startsAtUtc) =>
        EvaluatedForUtc == startsAtUtc
        && _assessments.Count == targets.Count
        && _assessments.TrueForAll(assessment => targets.Contains(assessment.Target));
    #endregion Domain Behavior
}
