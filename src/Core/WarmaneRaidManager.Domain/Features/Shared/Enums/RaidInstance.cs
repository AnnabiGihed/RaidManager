namespace WarmaneRaidManager.Domain.Features.Shared.Enums;

/// <summary>Identifies a supported WotLK raid instance.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Provides a closed domain vocabulary for identifies a supported WotLK raid instance.
/// </remarks>
public enum RaidInstance
{
    /// <summary>Identifies Naxxramas.</summary>
    Naxxramas = 1,
    /// <summary>Identifies the Obsidian Sanctum.</summary>
    ObsidianSanctum = 2,
    /// <summary>Identifies the Eye of Eternity.</summary>
    EyeOfEternity = 3,
    /// <summary>Identifies the Vault of Archavon.</summary>
    VaultOfArchavon = 4,
    /// <summary>Identifies Ulduar.</summary>
    Ulduar = 5,
    /// <summary>Identifies Trial of the Crusader.</summary>
    TrialOfTheCrusader = 6,
    /// <summary>Identifies Icecrown Citadel.</summary>
    IcecrownCitadel = 7,
    /// <summary>Identifies the Ruby Sanctum.</summary>
    RubySanctum = 8,
}
