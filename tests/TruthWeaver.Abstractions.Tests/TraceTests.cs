namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TraceTests
{
    [Fact]
    public void An_evaluated_entry_carries_its_resolved_result()
    {
        TraceEntry entry = new("hasRole(role: \"Y\")", TruthValue.True, NotEvaluated: false);

        Assert.Equal(TruthValue.True, entry.Result);
        Assert.False(entry.NotEvaluated);
    }

    [Fact]
    public void A_skipped_entry_has_no_result()
    {
        TraceEntry entry = new("isSuspended", Result: null, NotEvaluated: true);

        Assert.Null(entry.Result);
        Assert.True(entry.NotEvaluated);
    }

    [Fact]
    public void Trace_preserves_entries_in_evaluation_order()
    {
        TraceEntry first = new("isManager", TruthValue.True, false);
        TraceEntry second = new("isSuspended", null, true);

        Trace trace = new(new EquatableArray<TraceEntry>([first, second]));

        Assert.Equal([first, second], trace.Entries);
    }

    /// <summary>
    /// <see cref="Trace.Entries"/> is an <see cref="EquatableArray{T}"/> (ticket 28), so two traces built from
    /// separately-allocated but element-wise equal entry lists compare equal and share a hash code, instead of the
    /// reference equality a plain <see cref="IReadOnlyList{T}"/> member would give two different lists.
    /// </summary>
    [Fact]
    public void Trace_TwoInstancesWithEqualEntries_AreEqual_Test()
    {
        Trace first = new(new EquatableArray<TraceEntry>([new TraceEntry("isManager", TruthValue.True, false)]));
        Trace second = new(new EquatableArray<TraceEntry>([new TraceEntry("isManager", TruthValue.True, false)]));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
