namespace RaidManager.Domain.Features.Characters.Enums;

/// <summary>Identifies who in the community may see a character's profile.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Holds the two visibility choices of story #19 (owner decision, 2026-09-30); a profile is visible to the
/// community until its owner chooses otherwise (owner decision on #19, 2026-10-05).
/// </remarks>
public enum CharacterVisibility
{
    /// <summary>Every member of the community sees the profile.</summary>
    Community = 1,

    /// <summary>Only officers and raid leaders see the profile; officers always see characters that sign up.</summary>
    OfficersOnly = 2,
}
