namespace TruthWeaver.Abstractions;

/// <summary>
/// The <see cref="Fault.Exception"/> recorded when a <see cref="VariableReference"/> cannot be resolved (ADR-0006
/// decisions 6, 7 and 13). It names the reference and the kind of failure but never the resolved value, because the data
/// may be sensitive.
/// </summary>
/// <param name="reference">The reference that failed.</param>
/// <param name="kind">How it failed.</param>
/// <param name="message">A description of the failure that contains no data values.</param>
/// <param name="innerException">The exception a source threw, if any.</param>
public sealed class VariableResolutionException(
    VariableReference reference,
    VariableFailureKind kind,
    string message,
    Exception? innerException = null
) : Exception(message, innerException)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableResolutionException"/> class with no reference
    /// (an empty <see cref="Reference"/> and <see cref="VariableFailureKind.SourceError"/>); the engine always uses the full constructor.
    /// </summary>
    public VariableResolutionException()
        : this(new VariableReference(string.Empty, string.Empty), VariableFailureKind.SourceError, string.Empty) { }

    /// <summary>Initializes a new instance of the <see cref="VariableResolutionException"/> class with a message and no reference.</summary>
    /// <param name="message">The exception message.</param>
    public VariableResolutionException(string message)
        : this(new VariableReference(string.Empty, string.Empty), VariableFailureKind.SourceError, message) { }

    /// <summary>Initializes a new instance of the <see cref="VariableResolutionException"/> class with a message, a cause and no reference.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public VariableResolutionException(string message, Exception innerException)
        : this(new VariableReference(string.Empty, string.Empty), VariableFailureKind.SourceError, message, innerException) { }

    /// <summary>Gets the reference that failed.</summary>
    public VariableReference Reference { get; } = reference;

    /// <summary>Gets how the reference failed.</summary>
    public VariableFailureKind Kind { get; } = kind;
}
