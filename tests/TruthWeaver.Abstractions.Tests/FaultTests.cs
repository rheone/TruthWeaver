namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class FaultTests
{
    /// <summary>A fault records the faulting term and its exception.</summary>
    [Fact]
    public void Fault_Construction_RecordsTermAndException_Test()
    {
        TermIdentity term = new("hasRole", []);
        InvalidOperationException exception = new("boom");

        Fault fault = new(term, exception);

        Assert.Same(term, fault.Term);
        Assert.Same(exception, fault.Exception);
    }

    /// <summary>Two faults with equal terms and exceptions of the same type and message are equal, though the exception objects differ.</summary>
    [Fact]
    public void Equals_SameTermTypeAndMessage_IsEqual_Test()
    {
        Fault first = new(new TermIdentity("hasRole", []), new InvalidOperationException("boom"));
        Fault second = new(new TermIdentity("hasRole", []), new InvalidOperationException("boom"));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>A different message, exception type or term makes two faults unequal.</summary>
    [Fact]
    public void Equals_DifferentMessageTypeOrTerm_IsNotEqual_Test()
    {
        Fault fault = new(new TermIdentity("hasRole", []), new InvalidOperationException("boom"));

        Assert.NotEqual(fault, new Fault(new TermIdentity("hasRole", []), new InvalidOperationException("other")));
        Assert.NotEqual(fault, new Fault(new TermIdentity("hasRole", []), new ArgumentException("boom")));
        Assert.NotEqual(fault, new Fault(new TermIdentity("isManager", []), new InvalidOperationException("boom")));
    }

    /// <summary>A recorded exception stands for the type it recorded, so its fault equals the fault of the original exception.</summary>
    [Fact]
    public void Equals_RecordedExceptionOfTheSameTypeAndMessage_IsEqual_Test()
    {
        Fault original = new(new TermIdentity("hasRole", []), new InvalidOperationException("boom"));
        Fault recorded = new(
            new TermIdentity("hasRole", []),
            new RecordedFaultException("System.InvalidOperationException", "boom")
        );

        Assert.Equal(original, recorded);
        Assert.Equal("System.InvalidOperationException", recorded.ExceptionType);
    }

    /// <summary>Two variable failures with the same message but different references are not equal.</summary>
    [Fact]
    public void Equals_VariableFailuresWithDifferentReferences_IsNotEqual_Test()
    {
        TermIdentity term = new("hasRole", []);
        Fault first = new(
            term,
            new VariableResolutionException(new VariableReference("user", "$.a"), VariableFailureKind.Missing, "m")
        );
        Fault second = new(
            term,
            new VariableResolutionException(new VariableReference("user", "$.b"), VariableFailureKind.Missing, "m")
        );

        Assert.NotEqual(first, second);
    }
}
