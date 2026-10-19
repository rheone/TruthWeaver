namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// A clock that moves only when a test calls <see cref="Advance"/>. A one-shot timer fires when the clock reaches its due
/// time, so a pause such as <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)"/> needs no real wait.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> timers = [];
    private TimeSpan elapsed = TimeSpan.Zero;

    /// <summary>Gets the number of timers that wait to fire.</summary>
    public int PendingTimers
    {
        get
        {
            lock (this.timers)
            {
                return this.timers.Count(t => t.Due is not null);
            }
        }
    }

    /// <summary>Moves the clock forward and fires each timer that is now due, in due order.</summary>
    /// <param name="by">The time to move forward.</param>
    public void Advance(TimeSpan by)
    {
        List<ManualTimer> due;
        lock (this.timers)
        {
            this.elapsed += by;
            due = [.. this.timers.Where(t => t.Due <= this.elapsed).OrderBy(t => t.Due)];
        }

        foreach (ManualTimer timer in due)
        {
            timer.Fire();
        }
    }

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ManualTimer timer = new(this, callback, state);
        timer.Change(dueTime, period);
        lock (this.timers)
        {
            this.timers.Add(timer);
        }

        return timer;
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public TimeSpan? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            // Only one-shot timers are needed here, so the period is ignored.
            lock (owner.timers)
            {
                this.Due = dueTime == Timeout.InfiniteTimeSpan ? null : owner.elapsed + dueTime;
            }

            return true;
        }

        public void Fire()
        {
            this.Due = null;
            callback(state);
        }

        public void Dispose()
        {
            lock (owner.timers)
            {
                this.Due = null;
                _ = owner.timers.Remove(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
