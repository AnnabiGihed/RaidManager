namespace RaidManager.ApiService.Tests.Support;

/// <summary>Tells the system time moved by an offset a test sets, so expiries can be reached without waiting.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets the API tests reach the ten-minute pairing expiry and the 180-day companion expiry. API test classes
/// share one collection, which runs one test at a time, and a test that shifts the clock resets it when it ends.
/// </remarks>
public sealed class ShiftableClock : TimeProvider
{
    #region Properties
    /// <summary>Gets or sets how far the clock is ahead of the system time.</summary>
    public TimeSpan Offset { get; set; }
    #endregion Properties

    #region Overrides
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + Offset;
    #endregion Overrides
}
