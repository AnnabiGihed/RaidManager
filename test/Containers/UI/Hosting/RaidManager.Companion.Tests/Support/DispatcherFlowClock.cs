using Avalonia.Threading;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace RaidManager.Companion.Tests.Support;

/// <summary>A fake clock that runs the UI dispatcher until a flow has reached its next wait.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: In a headless test the code after a delay is posted to the UI dispatcher, possibly from another thread,
/// after <see cref="FakeTimeProvider.Advance"/> returns. This clock counts the timers the flow creates and runs the
/// dispatcher's jobs until the flow has started its next delay or finished, so the view tests never race the flow
/// (#514).
/// </remarks>
internal sealed class DispatcherFlowClock : FakeTimeProvider
{
    #region Fields
    /// <summary>Stores how long to wait for the flow before failing the test.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>Stores the number of timers given a due time.</summary>
    private int _timersCreated;

    /// <summary>Stores the number of times a timer fired.</summary>
    private int _timersFired;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="DispatcherFlowClock"/> class.</summary>
    /// <param name="start">The time the clock starts at.</param>
    public DispatcherFlowClock(DateTimeOffset start)
        : base(start)
    {
    }
    #endregion Constructors

    #region Public Methods
    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = base.CreateTimer(
            timerState =>
            {
                Interlocked.Increment(ref _timersFired);
                callback(timerState);
            },
            state,
            dueTime,
            period);
        CountIfArmed(dueTime);
        return new ArmedTimer(timer, this);
    }

    /// <summary>Advances the clock one second at a time; after a second that ended a wait, runs the dispatcher until the flow waits again or is done.</summary>
    /// <param name="seconds">The seconds.</param>
    /// <param name="isDone">Whether the flow has ended, so no new wait will come.</param>
    public void AdvanceSeconds(int seconds, Func<bool> isDone)
    {
        ArgumentNullException.ThrowIfNull(isDone);
        for (var second = 0; second < seconds; second++)
        {
            var timers = Volatile.Read(ref _timersCreated);
            var fired = Volatile.Read(ref _timersFired);
            Advance(TimeSpan.FromSeconds(1));
            if (Volatile.Read(ref _timersFired) == fired)
            {
                // No wait of the flow ended during this second, so there is nothing to catch up with.
                continue;
            }

            SpinWait.SpinUntil(
                () =>
                {
                    Dispatcher.UIThread.RunJobs();
                    return Volatile.Read(ref _timersCreated) > timers || isDone();
                },
                Patience).ShouldBeTrue("The pairing flow neither waited again nor ended after a second passed.");
        }
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Counts a timer once it has a due time, so a wait counts only when the clock will fire it.</summary>
    /// <param name="dueTime">The timer's due time.</param>
    private void CountIfArmed(TimeSpan dueTime)
    {
        if (dueTime != Timeout.InfiniteTimeSpan)
        {
            Interlocked.Increment(ref _timersCreated);
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
        private readonly DispatcherFlowClock _clock;

        /// <summary>Initializes a new instance of the <see cref="ArmedTimer"/> class.</summary>
        /// <param name="timer">The fake timer.</param>
        /// <param name="clock">The clock.</param>
        public ArmedTimer(ITimer timer, DispatcherFlowClock clock)
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
