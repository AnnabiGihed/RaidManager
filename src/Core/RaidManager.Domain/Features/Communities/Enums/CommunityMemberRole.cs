namespace RaidManager.Domain.Features.Communities.Enums;

/// <summary>Identifies the authorization role of a community member.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies the authorization role of a community member.
/// </remarks>
public enum CommunityMemberRole
{
    /// <summary>Identifies a regular community member.</summary>
    Member = 1,
    /// <summary>Identifies a member who can manage raids and rosters.</summary>
    RaidLeader = 2,
    /// <summary>Identifies a community officer.</summary>
    Officer = 3,
    /// <summary>Identifies a community administrator.</summary>
    Administrator = 4,
}
