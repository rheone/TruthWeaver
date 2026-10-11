namespace TruthWeaver.Testing;

/// <summary>
/// An exception that a predicate is allowed to throw during a <see cref="PredicateHarness"/> run. A matching exception
/// reports as <see cref="PredicateHarnessStatus.ExpectedFault"/>, not as a failure.
/// </summary>
/// <remarks>
/// An entry matches by exception type, by argument, or by both. An entry with an argument matches only the boundary-value
/// cases of that argument. The baseline call, the determinism repeats of the baseline and the cancellation call change no
/// single argument, so only an entry without an argument matches them.
/// </remarks>
public sealed class PredicateHarnessExpectedFault
{
    private PredicateHarnessExpectedFault(Type? exceptionType, string? argumentName)
    {
        this.ExceptionType = exceptionType;
        this.ArgumentName = argumentName;
    }

    /// <summary>
    /// Gets the exception type that matches, derived types included, or <see langword="null"/> when any exception type matches.
    /// </summary>
    public Type? ExceptionType { get; }

    /// <summary>
    /// Gets the argument whose boundary values may throw, or <see langword="null"/> when the entry matches for every argument.
    /// </summary>
    public string? ArgumentName { get; }

    /// <summary>Allows any exception from a boundary value of one argument.</summary>
    /// <param name="argumentName">The declared argument name.</param>
    /// <returns>The allow-list entry.</returns>
    /// <exception cref="ArgumentException"><paramref name="argumentName"/> is null, empty or white space.</exception>
    public static PredicateHarnessExpectedFault ForArgument(string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);
        return new(null, argumentName);
    }

    /// <summary>Allows an exception of type <typeparamref name="TException"/>, or a derived type, for every argument.</summary>
    /// <typeparam name="TException">The exception type, for example <see cref="ArgumentException"/>.</typeparam>
    /// <returns>The allow-list entry.</returns>
    public static PredicateHarnessExpectedFault OfType<TException>()
        where TException : Exception
    {
        return new(typeof(TException), null);
    }

    /// <summary>
    /// Allows an exception of type <typeparamref name="TException"/>, or a derived type, from a boundary value of one argument.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="argumentName">The declared argument name.</param>
    /// <returns>The allow-list entry.</returns>
    /// <exception cref="ArgumentException"><paramref name="argumentName"/> is null, empty or white space.</exception>
    public static PredicateHarnessExpectedFault OfType<TException>(string argumentName)
        where TException : Exception
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);
        return new(typeof(TException), argumentName);
    }

    /// <summary>Tests whether this entry allows <paramref name="exception"/> for a case that changes <paramref name="argumentName"/>.</summary>
    /// <param name="exception">The thrown exception.</param>
    /// <param name="argumentName">The argument the case changes, or <see langword="null"/> when the case changes none.</param>
    /// <returns><see langword="true"/> when the entry allows the exception.</returns>
    internal bool Matches(Exception exception, string? argumentName)
    {
        bool typeMatches = this.ExceptionType?.IsInstanceOfType(exception) != false;
        bool argumentMatches =
            this.ArgumentName is null || string.Equals(this.ArgumentName, argumentName, StringComparison.Ordinal);
        return typeMatches && argumentMatches;
    }
}
