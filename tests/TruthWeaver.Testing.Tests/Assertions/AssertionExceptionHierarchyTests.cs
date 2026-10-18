namespace TruthWeaver.Testing.Tests.Assertions;

using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Testing.Harness;

public sealed class AssertionExceptionHierarchyTests
{
    /// <summary>
    /// <see cref="DecisionAssertionException"/>, <see cref="PredicateHarnessException"/> and <see cref="RuleFuzzException"/>
    /// all derive from <see cref="TruthWeaverAssertionException"/>, so one catch handles every Testing failure.
    /// </summary>
    [Theory]
    [InlineData(typeof(DecisionAssertionException))]
    [InlineData(typeof(PredicateHarnessException))]
    [InlineData(typeof(RuleFuzzException))]
    public void TruthWeaverAssertionException_EveryTestingFailureType_IsDerivedFromIt_Test(Type exceptionType)
    {
        Assert.True(typeof(TruthWeaverAssertionException).IsAssignableFrom(exceptionType));
    }

    /// <summary>
    /// The base type is abstract and is an <see cref="Exception"/>, so a consumer never throws it directly.
    /// </summary>
    [Fact]
    public void TruthWeaverAssertionException_Type_IsAbstractException_Test()
    {
        Assert.True(typeof(TruthWeaverAssertionException).IsAbstract);
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(TruthWeaverAssertionException)));
    }
}
