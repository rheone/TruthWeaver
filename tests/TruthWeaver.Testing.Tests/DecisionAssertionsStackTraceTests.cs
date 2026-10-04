namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;

/// <summary>
/// A failed assertion should point at the test line that made it, so the assertion library's own frames stay out of
/// the stack trace.
/// </summary>
public sealed class DecisionAssertionsStackTraceTests
{
    /// <summary>The failing assertion method is hidden from the exception's stack trace.</summary>
    [Fact]
    public void BeSatisfied_FailingAssertion_HidesAssertionFrameFromStackTrace_Test()
    {
        Decision decision = new(TruthValue.False, []);

        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() => decision.Should().BeSatisfied());

        // Qualified with the type: the test method's own name also contains "BeSatisfied".
        string assertionFrame = $"{typeof(DecisionAssertions).FullName}.{nameof(DecisionAssertions.BeSatisfied)}";
        Assert.DoesNotContain(assertionFrame, exception.StackTrace, StringComparison.Ordinal);
    }
}
