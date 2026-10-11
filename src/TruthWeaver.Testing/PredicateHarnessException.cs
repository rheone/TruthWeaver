namespace TruthWeaver.Testing;

/// <summary>
/// Thrown by <see cref="PredicateHarnessReport.ShouldPass"/> when one or more harness checks fail. The message lists each
/// failure. Like <see cref="DecisionAssertionException"/>, it belongs to no test framework, so any runner that treats an
/// uncaught exception as a failure reports it.
/// </summary>
public sealed class PredicateHarnessException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PredicateHarnessException"/> class.</summary>
    public PredicateHarnessException() { }

    /// <summary>Initializes a new instance of the <see cref="PredicateHarnessException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    public PredicateHarnessException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="PredicateHarnessException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public PredicateHarnessException(string message, Exception innerException)
        : base(message, innerException) { }
}
