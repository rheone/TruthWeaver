namespace TruthWeaver.Abstractions.Tests;

using System.Diagnostics;
using System.Reflection;
using TruthWeaver.Abstractions;

public sealed class DecisionProjectTests
{
    /// <summary>A definite result passes through Project unchanged whichever value is chosen for Unknown.</summary>
    [Theory]
    [InlineData(TruthValue.True, TruthValue.False, TruthValue.True)]
    [InlineData(TruthValue.False, TruthValue.True, TruthValue.False)]
    public void Project_DefiniteResult_PassesThrough_Test(TruthValue result, TruthValue unknownAs, TruthValue expected)
    {
        Decision decision = new(result, []);

        Assert.Equal(expected, decision.Project(unknownAs));
    }

    /// <summary>An Unknown result becomes the value chosen for Unknown, and the decision stays unsatisfied.</summary>
    [Theory]
    [InlineData(TruthValue.True)]
    [InlineData(TruthValue.False)]
    public void Project_UnknownResult_BecomesTheChosenValue_Test(TruthValue unknownAs)
    {
        Decision decision = new(TruthValue.Unknown, []);

        Assert.Equal(unknownAs, decision.Project(unknownAs));
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>An argument that is not True or False is rejected for every result, not only an Unknown one.</summary>
    [Theory]
    [InlineData(TruthValue.True, TruthValue.Unknown)]
    [InlineData(TruthValue.False, TruthValue.Unknown)]
    [InlineData(TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData(TruthValue.True, (TruthValue)99)]
    public void Project_InvalidArgument_ThrowsArgumentOutOfRange_Test(TruthValue result, TruthValue unknownAs)
    {
        Decision decision = new(result, []);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => decision.Project(unknownAs));

        Assert.Equal("unknownAs", exception.ParamName);
    }

    /// <summary>The debugger display of a decision shows the result and the fault count.</summary>
    [Fact]
    public void DebuggerDisplay_Decision_ShowsResultAndFaultCount_Test()
    {
        Fault fault = new(new TermIdentity("a", []), new InvalidOperationException("boom"));
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([fault]));

        Assert.Equal("Unknown (1 faults)", DebuggerText(decision));
    }

    /// <summary>The debugger display of a fault names the term, the exception type and the message.</summary>
    [Fact]
    public void DebuggerDisplay_FaultWithoutScope_ShowsTermTypeAndMessage_Test()
    {
        Fault fault = new(new TermIdentity("a", []), new InvalidOperationException("boom"));

        Assert.Equal($"{fault.Term}: System.InvalidOperationException: boom", DebuggerText(fault));
    }

    /// <summary>The debugger display of a scoped fault starts with the scope.</summary>
    [Fact]
    public void DebuggerDisplay_FaultWithScope_StartsWithScope_Test()
    {
        Fault fault = new(new TermIdentity("a", []), new InvalidOperationException("boom"), "forall x");

        Assert.Equal($"forall x {fault.Term}: System.InvalidOperationException: boom", DebuggerText(fault));
    }

    /// <summary>A fault is not equal to null and equals itself.</summary>
    [Fact]
    public void Equals_NullAndSelf_NullIsNotEqualSelfIsEqual_Test()
    {
        Fault fault = new(new TermIdentity("a", []), new InvalidOperationException("boom"));

        Assert.False(fault.Equals(null));
        Assert.True(fault.Equals(fault));
    }

    // The display text is private by design; the attribute names it, so resolve it the way a debugger would.
    private static string DebuggerText(object value)
    {
        DebuggerDisplayAttribute attribute = value.GetType().GetCustomAttribute<DebuggerDisplayAttribute>()!;
        Assert.Equal("{DebuggerDisplay,nq}", attribute.Value);
        PropertyInfo property = value.GetType().GetProperty("DebuggerDisplay", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (string)property.GetValue(value)!;
    }
}
