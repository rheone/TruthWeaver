namespace TruthWeaver.Testing;

/// <summary>
/// The base type of every exception that a <c>TruthWeaver.Testing</c> assertion, harness report or fuzz report throws when a
/// check fails: <see cref="DecisionAssertionException"/>, <see cref="PredicateHarnessException"/> and
/// <see cref="RuleFuzzException"/>. A test can catch this one type to handle all of them. It belongs to no test framework,
/// so any runner that treats an uncaught exception as a failure reports it.
/// </summary>
public abstract class TruthWeaverAssertionException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="TruthWeaverAssertionException"/> class.</summary>
    protected TruthWeaverAssertionException() { }

    /// <summary>Initializes a new instance of the <see cref="TruthWeaverAssertionException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    protected TruthWeaverAssertionException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="TruthWeaverAssertionException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    protected TruthWeaverAssertionException(string message, Exception innerException)
        : base(message, innerException) { }
}
