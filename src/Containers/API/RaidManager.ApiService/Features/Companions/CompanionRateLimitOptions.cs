using System.ComponentModel.DataAnnotations;

namespace RaidManager.ApiService.Features.Companions;

/// <summary>Configures the rate limits on pairing, from the <c>Companion:RateLimits</c> section.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Bounds what an address can ask of the public pairing routes and how many codes a player can try (ADR-0030, threat model); the defaults suit a normal pairing with room for retries.
/// </remarks>
public sealed class CompanionRateLimitOptions
{
    #region Constants
    /// <summary>Defines the configuration section.</summary>
    public const string SectionName = "Companion:RateLimits";
    #endregion Constants

    #region Properties
    /// <summary>Gets or sets how many pairings one address may start per <see cref="PairingStartWindow"/>.</summary>
    [Range(1, 10_000)]
    public int PairingStartsPerWindow { get; set; } = 10;

    /// <summary>Gets or sets the window of <see cref="PairingStartsPerWindow"/>.</summary>
    public TimeSpan PairingStartWindow { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Gets or sets how many token polls one address may make per <see cref="TokenPollWindow"/>.</summary>
    [Range(1, 10_000)]
    public int TokenPollsPerWindow { get; set; } = 60;

    /// <summary>Gets or sets the window of <see cref="TokenPollsPerWindow"/>.</summary>
    public TimeSpan TokenPollWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets how many codes one player may look up or confirm per <see cref="CodeCheckWindow"/>.</summary>
    [Range(1, 10_000)]
    public int CodeChecksPerWindow { get; set; } = 10;

    /// <summary>Gets or sets the window of <see cref="CodeChecksPerWindow"/>.</summary>
    public TimeSpan CodeCheckWindow { get; set; } = TimeSpan.FromMinutes(10);
    #endregion Properties
}
