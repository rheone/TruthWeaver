namespace TruthWeaver.Abstractions;

/// <summary>
/// The <see cref="Fault.Exception"/> of a fault read back from a stored decision record. The original exception object
/// is not stored, only its type name and message, so a read fault carries this exception instead. <see cref="Fault"/>
/// equality reads <see cref="ExceptionType"/> in place of the runtime type, so a read fault equals the fault that was
/// written.
/// </summary>
/// <example>
/// The type name is set by name, so it cannot be swapped with the message:
/// <code>
/// RecordedFaultException recorded = new("Directory unavailable.") { ExceptionType = "System.InvalidOperationException" };
/// </code>
/// </example>
public sealed class RecordedFaultException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with an empty message.</summary>
    public RecordedFaultException()
        : base(string.Empty) { }

    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with the recorded message.</summary>
    /// <param name="message">The message of the exception that was recorded.</param>
    public RecordedFaultException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with a message and a cause.</summary>
    /// <param name="message">The message of the exception that was recorded.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public RecordedFaultException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Gets the full type name of the exception that was recorded, for example <c>System.InvalidOperationException</c>.
    /// Empty unless it is set.
    /// </summary>
    public string ExceptionType { get; init; } = string.Empty;
}
