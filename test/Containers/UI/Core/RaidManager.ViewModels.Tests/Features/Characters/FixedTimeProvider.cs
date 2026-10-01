namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Returns one fixed instant as the current time.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Makes the "found" labels deterministic.
/// </remarks>
internal sealed class FixedTimeProvider : TimeProvider
{
    #region Fields
    /// <summary>Stores the instant returned as now.</summary>
    private readonly DateTimeOffset _now;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="FixedTimeProvider"/> class.</summary>
    /// <param name="now">The instant returned as now.</param>
    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }
    #endregion Constructors

    #region Overrides
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;
    #endregion Overrides
}
