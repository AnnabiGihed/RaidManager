using Microsoft.Extensions.Time.Testing;

namespace RaidManager.Web.Tests.Support;

/// <summary>A fake clock that tells when a delay has been scheduled on it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Lets a page test advance time only once the page waits on a delay: advancing before the delay is scheduled
/// would start it from the new time, and the test would race the page (the lesson of #514, avalonia-tests §5).
/// </remarks>
internal sealed class ArmedTimeProvider : FakeTimeProvider
{
    #region Fields
    /// <summary>Stores the number of timers given a due time.</summary>
    private int _armed;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ArmedTimeProvider"/> class.</summary>
    /// <param name="start">The time the clock starts at.</param>
    public ArmedTimeProvider(DateTimeOffset start)
        : base(start)
    {
    }
    #endregion Constructors

    #region Public Properties
    /// <summary>Gets the number of timers given a due time so far.</summary>
    public int Armed => Volatile.Read(ref _armed);
    #endregion Public Properties

    #region Public Methods
    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        CountIfArmed(dueTime);
        return new ArmedTimer(timer, this);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Counts a timer once it has a due time.</summary>
    /// <param name="dueTime">The timer's due time.</param>
    private void CountIfArmed(TimeSpan dueTime)
    {
        if (dueTime != Timeout.InfiniteTimeSpan)
        {
            Interlocked.Increment(ref _armed);
        }
    }
    #endregion Private Helpers

    #region Nested Types
    /// <summary>Forwards to the fake timer, and counts it when it is given a due time later.</summary>
    private sealed class ArmedTimer : ITimer
    {
        /// <summary>Stores the fake timer.</summary>
        private readonly ITimer _timer;

        /// <summary>Stores the clock that counts armed timers.</summary>
        private readonly ArmedTimeProvider _clock;

        /// <summary>Initializes a new instance of the <see cref="ArmedTimer"/> class.</summary>
        /// <param name="timer">The fake timer.</param>
        /// <param name="clock">The clock.</param>
        public ArmedTimer(ITimer timer, ArmedTimeProvider clock)
        {
            _timer = timer;
            _clock = clock;
        }

        /// <inheritdoc />
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            var changed = _timer.Change(dueTime, period);
            _clock.CountIfArmed(dueTime);
            return changed;
        }

        /// <inheritdoc />
        public void Dispose() => _timer.Dispose();

        /// <inheritdoc />
        public ValueTask DisposeAsync() => _timer.DisposeAsync();
    }
    #endregion Nested Types
}
