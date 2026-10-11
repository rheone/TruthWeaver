namespace TruthWeaver.Tool.Core;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Diagnostics;

/// <summary>
/// The result of a tool core operation: a value, or no value, and every diagnostic the operation raised. The core never
/// throws for an expected failure (a missing file, a malformed document, a rule that does not compile); it returns the
/// failure here.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="Value">The value, or <see langword="null"/> when the operation failed.</param>
/// <param name="Diagnostics">Every diagnostic raised, including warnings and information on success.</param>
public sealed record ToolResult<T>(T? Value, EquatableArray<Diagnostic> Diagnostics)
    where T : class
{
    /// <summary>
    /// Gets a value indicating whether the operation produced a value and raised no error. When it is
    /// <see langword="true"/>, <see cref="Value"/> is not <see langword="null"/>.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool Succeeded => this.Value is not null && !this.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>Gets the value and the diagnostics without a result object.</summary>
    /// <param name="value">The value when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised.</param>
    /// <returns>The value of <see cref="Succeeded"/>.</returns>
    public bool TryGetValue([NotNullWhen(true)] out T? value, out EquatableArray<Diagnostic> diagnostics)
    {
        diagnostics = this.Diagnostics;
        value = this.Succeeded ? this.Value : null;
        return value is not null;
    }
}
