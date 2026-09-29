namespace RaidManager.Domain.Features.Characters.Enums;

/// <summary>Identifies the origin of synchronized character data.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies the origin of synchronized character data.
/// </remarks>
public enum CharacterDataSource
{
    /// <summary>Identifies data retrieved from the Warmane Armory.</summary>
    WarmaneArmory = 1,
    /// <summary>Identifies data synchronized from the WoW addon.</summary>
    WowAddon = 2,
    /// <summary>Identifies data calculated by the platform from trusted source values.</summary>
    Derived = 3,
}
