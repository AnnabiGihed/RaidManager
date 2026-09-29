namespace WarmaneRaidManager.Domain.Features.Characters.ValueObjects;

/// <summary>Represents the calculated GearScore for one character loadout.</summary>
/// <remarks>
/// Author: Rodolphe Balay<br/>
/// Date: 2026-09-29<br/>
/// Purpose: Prevents a GearScore from being represented as an unvalidated primitive detached from its loadout.
/// </remarks>
public readonly record struct GearScore
{
    #region Constants
    /// <summary>Defines the largest GearScore accepted by the platform to reject corrupted synchronization payloads.</summary>
    private const int MaxSupportedGearScore = 10000;
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="GearScore"/> struct.</summary>
    /// <param name="value">The calculated GearScore.</param>
    /// <exception cref="DomainException">Thrown when the GearScore is negative or implausibly large.</exception>
    public GearScore(int value)
    {
        if (value < 0 || value > MaxSupportedGearScore)
        {
            throw new DomainException("GearScore must be between 0 and 10000.");
        }

        Value = value;
    }
    #endregion Constructors

    #region Properties
    /// <summary>Gets the calculated GearScore value.</summary>
    public int Value { get; }
    #endregion Properties

    #region Overrides
    /// <summary>Returns the GearScore as text.</summary>
    /// <returns>The decimal GearScore.</returns>
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    #endregion Overrides
}
