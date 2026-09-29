namespace WarmaneRaidManager.Domain.Features.Shared.Enums;

/// <summary>Identifies a WotLK raid size and difficulty.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies a WotLK raid size and difficulty.
/// </remarks>
public enum RaidDifficulty
{
    /// <summary>Identifies ten-player normal difficulty.</summary>
    TenPlayer = 1,
    /// <summary>Identifies ten-player heroic difficulty where the raid supports it.</summary>
    TenPlayerHeroic = 2,
    /// <summary>Identifies twenty-five-player normal difficulty.</summary>
    TwentyFivePlayer = 3,
    /// <summary>Identifies twenty-five-player heroic difficulty where the raid supports it.</summary>
    TwentyFivePlayerHeroic = 4,
}
