namespace TruthWeaver.Abstractions;

/// <summary>
/// The <see cref="Fault.Exception"/> of a fault read back from a stored decision record. The original exception object
/// is not stored, only its type name and message, so a read fault carries this exception instead. <see cref="Fault"/>
/// equality reads <see cref="ExceptionType"/> in place of the runtime type, so a read fault equals the fault that was
/// written.
/// </summary>
public sealed class RecordedFaultException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with an empty type name and message.</summary>
    public RecordedFaultException()
        : this(string.Empty, string.Empty) { }

    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with a message and an empty type name.</summary>
    /// <param name="message">The recorded exception message.</param>
    public RecordedFaultException(string message)
        : this(string.Empty, message) { }

    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with a message, a cause and an empty type name.</summary>
    /// <param name="message">The recorded exception message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public RecordedFaultException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.ExceptionType = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="RecordedFaultException"/> class with the recorded type name and message.</summary>
    /// <param name="exceptionType">The full type name of the exception that was recorded, for example <c>System.InvalidOperationException</c>.</param>
    /// <param name="message">The message of the exception that was recorded.</param>
    public RecordedFaultException(string exceptionType, string message)
        : base(message)
    {
        this.ExceptionType = exceptionType;
    }

    /// <summary>Gets the full type name of the exception that was recorded.</summary>
    public string ExceptionType { get; }
}
