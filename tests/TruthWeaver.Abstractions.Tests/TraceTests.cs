namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TraceTests
{
    /// <summary>An evaluated entry carries its resolved result.</summary>
    [Fact]
    public void Entry_Evaluated_CarriesResolvedResult_Test()
    {
        TraceEntry entry = new("hasRole(role: \"Y\")", TruthValue.True, NotEvaluated: false);

        Assert.Equal(TruthValue.True, entry.Result);
        Assert.False(entry.NotEvaluated);
    }

    /// <summary>A skipped entry has no result.</summary>
    [Fact]
    public void Entry_Skipped_HasNoResult_Test()
    {
        TraceEntry entry = new("isSuspended", Result: null, NotEvaluated: true);

        Assert.Null(entry.Result);
        Assert.True(entry.NotEvaluated);
    }

    /// <summary>A trace preserves its entries in evaluation order.</summary>
    [Fact]
    public void Entries_EvaluationOrder_IsPreserved_Test()
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
