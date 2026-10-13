namespace TruthWeaver.Testing.Tests;

public sealed class SimulatedPredicateFaultExceptionTests
{
    /// <summary>The parameterless constructor produces the default state.</summary>
    [Fact]
    public void Constructor_Parameterless_ProducesDefaultState_Test()
    {
        SimulatedPredicateFaultException exception = new();

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Null(exception.InnerException);
    }

    /// <summary>The message constructor sets the message.</summary>
    [Fact]
    public void Constructor_Message_SetsMessage_Test()
    {
        SimulatedPredicateFaultException exception = new("predicate fault");

        Assert.Equal("predicate fault", exception.Message);
        Assert.Null(exception.InnerException);
    }

    /// <summary>The message and inner exception constructor sets both.</summary>
    [Fact]
    public void Constructor_MessageAndInnerException_SetsBoth_Test()
    {
        InvalidOperationException inner = new("boom");

        SimulatedPredicateFaultException exception = new("predicate fault", inner);

        Assert.Equal("predicate fault", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }
}
