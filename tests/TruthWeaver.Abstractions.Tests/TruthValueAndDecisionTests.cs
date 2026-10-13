namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TruthValueAndDecisionTests
{
    // Fault.Exception compares by reference (the default for Exception); Decision_TwoInstancesWithEqualFaultsAndTrace_AreEqual_Test
    // reuses this one instance for both decisions' faults so the test is about the list members' equality, not the exception's.
    private static readonly InvalidOperationException SharedException = new("boom");

    /// <summary>A decision is satisfied only when its result is true.</summary>
    [Fact]
    public void Decision_IsSatisfied_OnlyWhenResultIsTrue_Test()
    {
        Decision trueDecision = new(TruthValue.True, []);
        Decision falseDecision = new(TruthValue.False, []);
        Decision unknownDecision = new(TruthValue.Unknown, []);

        Assert.True(trueDecision.IsSatisfied);
        Assert.False(falseDecision.IsSatisfied);
        Assert.False(unknownDecision.IsSatisfied);
    }

    /// <summary>A decision carries its faults in recorded order.</summary>
    [Fact]
    public void Decision_Faults_CarryInRecordedOrder_Test()
    {
        Fault first = new(new TermIdentity("a", []), new InvalidOperationException("first"));
        Fault second = new(new TermIdentity("b", []), new InvalidOperationException("second"));

        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([first, second]));

        Assert.Equal([first, second], decision.Faults);
    }

    /// <summary>A decision trace and evaluated tree default to null.</summary>
    [Fact]
    public void Decision_Defaults_TraceAndEvaluatedTreeAreNull_Test()
    {
        Decision decision = new(TruthValue.True, []);

        Assert.Null(decision.Trace);
        Assert.Null(decision.TraceTree);
    }

    /// <summary>Collapse resolves <c>Unknown</c> according to the policy, leaving definite values unchanged.</summary>
    [Theory]
    [InlineData(TruthValue.True, CollapsePolicy.UnknownAsFalse, CollapseOutcome.True)]
    [InlineData(TruthValue.True, CollapsePolicy.UnknownAsTrue, CollapseOutcome.True)]
    [InlineData(TruthValue.True, CollapsePolicy.UnknownIsError, CollapseOutcome.True)]
    [InlineData(TruthValue.False, CollapsePolicy.UnknownAsFalse, CollapseOutcome.False)]
    [InlineData(TruthValue.False, CollapsePolicy.UnknownAsTrue, CollapseOutcome.False)]
    [InlineData(TruthValue.False, CollapsePolicy.UnknownIsError, CollapseOutcome.False)]
    [InlineData(TruthValue.Unknown, CollapsePolicy.UnknownAsFalse, CollapseOutcome.False)]
    [InlineData(TruthValue.Unknown, CollapsePolicy.UnknownAsTrue, CollapseOutcome.True)]
    [InlineData(TruthValue.Unknown, CollapsePolicy.UnknownIsError, CollapseOutcome.RejectedUnresolved)]
    public void Collapse_ByPolicy_ResolvesOnlyUnknown_Test(TruthValue result, CollapsePolicy policy, CollapseOutcome expected)
    {
        Decision decision = new(result, []);

        Assert.Equal(expected, decision.Collapse(policy));
    }

    /// <summary>Collapse throws for an undefined policy.</summary>
    [Fact]
    public void Collapse_UndefinedPolicy_ThrowsArgumentOutOfRange_Test()
    {
        Decision decision = new(TruthValue.Unknown, []);

        Assert.Throws<ArgumentOutOfRangeException>(() => decision.Collapse((CollapsePolicy)99));
    }

    /// <summary>Collapse never modifies the decision itself; it leaves the result and faults unchanged.</summary>
    [Fact]
    public void Collapse_AnyPolicy_LeavesDecisionAndFailClosedSatisfactionUnchanged_Test()
    {
        Fault fault = new(new TermIdentity("a", []), new InvalidOperationException("boom"));
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([fault]));

        CollapseOutcome outcome = decision.Collapse(CollapsePolicy.UnknownAsTrue);

        Assert.Equal(CollapseOutcome.True, outcome);
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Assert.Equal([fault], decision.Faults);
    }

    /// <summary>Collapse with <c>UnknownIsError</c> rejects but records no fault in the decision.</summary>
    [Fact]
    public void Collapse_RejectedPolicy_RecordsNoFault_Test()
    {
        Decision decision = new(TruthValue.Unknown, []);

        CollapseOutcome outcome = decision.Collapse(CollapsePolicy.UnknownIsError);

        Assert.Equal(CollapseOutcome.RejectedUnresolved, outcome);
        Assert.Empty(decision.Faults);
    }

    /// <summary>
    /// <see cref="Decision.Faults"/> is an <see cref="EquatableArray{T}"/> (ticket 28), so two decisions built from
    /// separately-allocated but element-wise equal fault lists (and equal trace members) compare equal and share a
    /// hash code, instead of the reference equality a plain <see cref="IReadOnlyList{T}"/> member would give two
    /// different lists.
    /// </summary>
    [Fact]
    public void Decision_TwoInstancesWithEqualFaultsAndTrace_AreEqual_Test()
    {
        TermIdentity term = new("isManager", []);
        Fault firstFault = new(term, SharedException);
        Fault secondFault = new(term, SharedException);
        TraceEntry entry = new("isManager", TruthValue.True, false);
        TraceNode node = new("isManager", TruthValue.True, false, EquatableArray<TraceNode>.Empty);

        Decision first = new(
            TruthValue.Unknown,
            new EquatableArray<Fault>([firstFault]),
            new Trace(new EquatableArray<TraceEntry>([entry])),
            node
        );
        Decision second = new(
            TruthValue.Unknown,
            new EquatableArray<Fault>([secondFault]),
            new Trace(new EquatableArray<TraceEntry>([entry])),
            node
        );

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
