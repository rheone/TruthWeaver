namespace TruthWeaver.Building;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;

/// <summary>
/// What <see cref="DataSourceExtensions.TryGetAsync{T}"/> answers: the value read from a data source, or the reason it
/// could not be read, described as data. It uses <see cref="VariableFailureKind"/>, the same vocabulary a variable
/// failure has at evaluation time.
/// </summary>
/// <typeparam name="T">The requested value type.</typeparam>
public sealed class DataReadResult<T>
{
    private readonly T? value;

    private DataReadResult(T? value, VariableFailureKind? failureKind, string? errorMessage)
    {
        this.value = value;
        this.FailureKind = failureKind;
        this.ErrorMessage = errorMessage;
    }

    /// <summary>Gets why the read failed, or <see langword="null"/> when it succeeded.</summary>
    public VariableFailureKind? FailureKind { get; }

    /// <summary>Gets a description of the failure, or <see langword="null"/> when the read succeeded. It never contains data values.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets a value indicating whether <see cref="Value"/> holds the value read.</summary>
    [MemberNotNullWhen(false, nameof(FailureKind), nameof(ErrorMessage))]
    public bool Succeeded => this.FailureKind is null;

    /// <summary>Gets the value read.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Succeeded"/> is <see langword="false"/>.</exception>
    public T Value =>
        this.Succeeded
            ? this.value!
            : throw new InvalidOperationException("The read failed, so there is no value; check Succeeded first.");

    internal static DataReadResult<T> Success(T value)
    {
        return new(value, null, null);
    }

    internal static DataReadResult<T> Failure(VariableFailureKind kind, string message)
    {
        return new(default, kind, message);
    }
}
