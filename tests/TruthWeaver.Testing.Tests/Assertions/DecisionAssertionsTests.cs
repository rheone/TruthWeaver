namespace TruthWeaver.Testing.Tests.Assertions;

using TruthWeaver.Abstractions;

public sealed class DecisionAssertionsTests
{
    /// <summary>BeSatisfied does not throw for a true result.</summary>
    [Fact]
    public void BeSatisfied_TrueResult_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.True, []);

        decision.Should().BeSatisfied();
    }

    /// <summary>BeSatisfied throws for a false result.</summary>
    [Fact]
    public void BeSatisfied_FalseResult_Throws_Test()
    {
        Decision decision = new(TruthValue.False, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().BeSatisfied());
    }

    /// <summary>BeSatisfied throws for an unknown result.</summary>
    [Fact]
    public void BeSatisfied_UnknownResult_Throws_Test()
    {
        // Decision.IsSatisfied fails closed on Unknown (ADR-0001), and the assertion mirrors that.
        Decision decision = new(TruthValue.Unknown, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().BeSatisfied());
    }

    /// <summary>NotBeSatisfied does not throw for a false result.</summary>
    [Fact]
    public void NotBeSatisfied_FalseResult_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.False, []);

        decision.Should().NotBeSatisfied();
    }

    /// <summary>NotBeSatisfied throws for a true result.</summary>
    [Fact]
    public void NotBeSatisfied_TrueResult_Throws_Test()
    {
        Decision decision = new(TruthValue.True, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().NotBeSatisfied());
    }

    /// <summary>HaveResult does not throw for a matching result.</summary>
    [Fact]
    public void HaveResult_MatchingResult_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.Unknown, []);

        decision.Should().HaveResult(TruthValue.Unknown);
    }

    /// <summary>HaveResult throws for a mismatched result.</summary>
    [Fact]
    public void HaveResult_MismatchedResult_Throws_Test()
    {
        Decision decision = new(TruthValue.Unknown, []);

        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            decision.Should().HaveResult(TruthValue.True)
        );
        Assert.Contains("True", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Unknown", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>HaveNoFaults does not throw for an empty fault list.</summary>
    [Fact]
    public void HaveNoFaults_EmptyFaultList_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.True, []);

        decision.Should().HaveNoFaults();
    }

    /// <summary>HaveNoFaults throws when faults are present.</summary>
    [Fact]
    public void HaveNoFaults_WithFaults_Throws_Test()
    {
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([SampleFault()]));

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveNoFaults());
    }

    /// <summary>HaveFault does not throw when faults are present.</summary>
    [Fact]
    public void HaveFault_WithFaults_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([SampleFault()]));

        decision.Should().HaveFault();
    }

    /// <summary>HaveFault throws for an empty fault list.</summary>
    [Fact]
    public void HaveFault_EmptyFaultList_Throws_Test()
    {
        Decision decision = new(TruthValue.True, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveFault());
    }

    /// <summary>HaveFaultCount does not throw when the count matches.</summary>
    [Fact]
    public void HaveFaultCount_MatchingCount_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([SampleFault(), SampleFault()]));

        decision.Should().HaveFaultCount(2);
    }

    /// <summary>HaveFaultCount throws when the count does not match.</summary>
    [Fact]
    public void HaveFaultCount_MismatchedCount_Throws_Test()
    {
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([SampleFault()]));

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveFaultCount(2));
    }

    /// <summary>HaveFaultForPredicate does not throw when a fault names the predicate.</summary>
    [Fact]
    public void HaveFaultForPredicate_MatchingPredicateName_DoesNotThrow_Test()
    {
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([SampleFault("hasRole")]));

        decision.Should().HaveFaultForPredicate("hasRole");
    }

    /// <summary>HaveFaultForPredicate throws when no fault names the predicate.</summary>
    [Fact]
    public void HaveFaultForPredicate_NoMatchingPredicateName_Throws_Test()
    {
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([SampleFault("hasRole")]));

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveFaultForPredicate("isManager"));
    }

    /// <summary>Decision assertions chain fluently.</summary>
    [Fact]
    public void Assertions_Chain_ChainFluently_Test()
    {
        Decision decision = new(TruthValue.True, []);

        decision.Should().HaveResult(TruthValue.True).BeSatisfied().HaveNoFaults();
    }

    private static Fault SampleFault(string predicateName = "hasRole")
    {
        return new Fault(new TermIdentity(predicateName, []), new InvalidOperationException("simulated"));
    }
}
