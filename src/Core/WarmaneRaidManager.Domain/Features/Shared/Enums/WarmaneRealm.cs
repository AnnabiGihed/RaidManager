namespace WarmaneRaidManager.Domain.Features.Shared.Enums;

/// <summary>Identifies the supported Warmane realm for a character or raid community.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies the supported Warmane realm for a character or raid community.
/// </remarks>
public enum WarmaneRealm
{
    /// <summary>Identifies the permanent Icecrown WotLK realm.</summary>
    Icecrown = 1,
    /// <summary>Identifies the permanent Lordaeron WotLK realm.</summary>
    Lordaeron = 2,
    /// <summary>Identifies the Blackrock PvP realm.</summary>
    Blackrock = 3,
    /// <summary>Identifies the seasonal Onyxia progression realm.</summary>
    Onyxia = 4,
}
