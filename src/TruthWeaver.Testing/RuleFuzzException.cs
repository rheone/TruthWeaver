namespace TruthWeaver.Testing;

/// <summary>
/// Thrown by <see cref="RuleFuzzReport.ShouldPass"/> when one or more fuzz checks fail. The message has the seed and lists
/// each failure. It belongs to no test framework, so any runner that treats an uncaught exception as a failure reports it.
/// </summary>
public sealed class RuleFuzzException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RuleFuzzException"/> class.</summary>
    public RuleFuzzException() { }

    /// <summary>Initializes a new instance of the <see cref="RuleFuzzException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    public RuleFuzzException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="RuleFuzzException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public RuleFuzzException(string message, Exception innerException)
        : base(message, innerException) { }
}
