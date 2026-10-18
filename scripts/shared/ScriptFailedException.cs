namespace TruthWeaver.Scripts;

/// <summary>
/// A script failure that carries the exit code the script should return. Scripts catch it once at the top
/// level, print the message to standard error and return <see cref="ExitCode"/>.
/// </summary>
public sealed class ScriptFailedException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ScriptFailedException"/> class.</summary>
    public ScriptFailedException()
        : this("The script failed.", 1) { }

    /// <summary>Initializes a new instance of the <see cref="ScriptFailedException"/> class.</summary>
    /// <param name="message">What went wrong, for standard error.</param>
    public ScriptFailedException(string message)
        : this(message, 1) { }

    /// <summary>Initializes a new instance of the <see cref="ScriptFailedException"/> class.</summary>
    /// <param name="message">What went wrong, for standard error.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ScriptFailedException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="ScriptFailedException"/> class.</summary>
    /// <param name="message">What went wrong, for standard error.</param>
    /// <param name="exitCode">The exit code the script returns.</param>
    public ScriptFailedException(string message, int exitCode)
        : base(message)
    {
        this.ExitCode = exitCode;
    }

    /// <summary>Gets the exit code the script returns.</summary>
    public int ExitCode { get; } = 1;
}
