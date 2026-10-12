namespace TruthWeaver.Testing;

using TruthWeaver.Compilation;

/// <summary>
/// Thrown by <see cref="RuleTestReport.ShouldPass"/> when a case of a rule test failed, and by
/// <see cref="RuleTestFile.RunAsync(string, CompilerOptions?, CancellationToken)"/> when the file is not usable.
/// </summary>
public sealed class RuleTestException : TruthWeaverAssertionException
{
    /// <summary>Initializes a new instance of the <see cref="RuleTestException"/> class.</summary>
    public RuleTestException() { }

    /// <summary>Initializes a new instance of the <see cref="RuleTestException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    public RuleTestException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="RuleTestException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public RuleTestException(string message, Exception innerException)
        : base(message, innerException) { }
}
