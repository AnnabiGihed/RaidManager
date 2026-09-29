namespace RaidManager.Domain.Features.Characters.Enums;

/// <summary>Identifies the review state of a request to associate a character with a user.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Makes character ownership an explicit, reviewed decision instead of a side effect of a snapshot upload.
/// </remarks>
public enum CharacterClaimState
{
    /// <summary>Identifies a discovered character awaiting the player's approval or rejection.</summary>
    Pending = 1,

    /// <summary>Identifies a claim the player approved; the user owns the character.</summary>
    Approved = 2,

    /// <summary>Identifies a claim the player rejected; repeated uploads do not reopen it.</summary>
    Rejected = 3,

    /// <summary>Identifies a claim blocked because another identity already owns the character.</summary>
    Conflict = 4,
}
