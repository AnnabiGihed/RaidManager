namespace RaidManager.Web.Tests.Support;

/// <summary>Tells the website's session the time, and lets a test move it forward.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-03<br/>
/// Purpose: Lets the sign-in tests prove the session lifetime without waiting for it.
/// </remarks>
public sealed class TestClock : TimeProvider
{
    #region Fields
    /// <summary>Stores the current time.</summary>
    private DateTimeOffset _now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
    #endregion Fields

    #region Public Methods
    /// <summary>Moves the clock forward.</summary>
    /// <param name="duration">How far to move it.</param>
    public void Advance(TimeSpan duration) => _now += duration;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;
    #endregion Public Methods
}
