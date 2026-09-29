using RaidManager.Domain.Features.Raids.Enums;

namespace RaidManager.Domain.Features.Raids.ValueObjects;

/// <summary>Represents the raid-start readiness verdict of one character for one required raid target.</summary>
/// <param name="Target">The required instance and difficulty.</param>
/// <param name="Verdict">The readiness verdict at the scheduled raid start.</param>
/// <param name="ResetsAtUtc">The reset instant of the matching save, when one is known.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Gives every target of a combined raid its own explainable verdict instead of one opaque eligibility flag.
/// </remarks>
public sealed record EligibilityAssessment(RaidTarget Target, ReadinessVerdict Verdict, DateTimeOffset? ResetsAtUtc);
