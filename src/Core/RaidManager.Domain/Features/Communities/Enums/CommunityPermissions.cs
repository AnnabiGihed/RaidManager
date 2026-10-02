namespace RaidManager.Domain.Features.Communities.Enums;

/// <summary>Names what a community role allows its members to do.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-02<br/>
/// Purpose: The permissions a role can give (owner decisions on #308). A member has every permission of every role
/// their Discord roles give them; the Administrator has them all.
/// </remarks>
[Flags]
public enum CommunityPermissions
{
    /// <summary>Allows nothing beyond being a Member: signing up for raids.</summary>
    None = 0,

    /// <summary>Allows creating and editing raids, templates and recurrence, and locking or reopening signups.</summary>
    ManageRaids = 1,

    /// <summary>Allows selecting and swapping participants, recording exceptions, publishing and setting boss assignments.</summary>
    BuildRosters = 2,

    /// <summary>Allows recording attendance and exporting the roster to the addon.</summary>
    RunRaidNight = 4,

    /// <summary>Allows deciding character claims another player already owns.</summary>
    ReviewConflicts = 8,

    /// <summary>Allows creating, editing and deleting roles and mapping Discord roles to them.</summary>
    ManageCommunityRoles = 16,

    /// <summary>Every permission, as the Administrator has.</summary>
    All = ManageRaids | BuildRosters | RunRaidNight | ReviewConflicts | ManageCommunityRoles,
}
