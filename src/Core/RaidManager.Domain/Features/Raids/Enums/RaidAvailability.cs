namespace RaidManager.Domain.Features.Raids.Enums;

/// <summary>Identifies whether a participant plans to attend a raid.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Keeps a player's availability separate from officer decisions; selection and bench belong to a composition
/// and absence belongs to attendance.
/// </remarks>
public enum RaidAvailability
{
    /// <summary>Identifies a participant who plans to attend with one of the offered options.</summary>
    Confirmed = 1,

    /// <summary>Identifies a participant who may attend.</summary>
    Tentative = 2,

    /// <summary>Identifies a participant who expects to arrive after raid start.</summary>
    Late = 3,

    /// <summary>Identifies a participant who does not plan to attend.</summary>
    Declined = 4,
}
