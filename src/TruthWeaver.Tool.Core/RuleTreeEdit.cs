namespace TruthWeaver.Tool.Core;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;

/// <summary>The result of one edit of <see cref="RuleTreeEditor"/>: the edited rule, or the reason the edit was refused.</summary>
/// <param name="Rule">The edited rule, compiled; <see langword="null"/> when the edit was refused.</param>
/// <param name="Path">The path of the node to select after the edit, such as the new operand of an insert, or <see langword="null"/>.</param>
/// <param name="Message">What the edit did, or why it was refused.</param>
/// <param name="Diagnostics">The compiler diagnostics that refused the edit or the expression it was given; empty otherwise.</param>
public sealed record RuleTreeEdit(
    CompiledRule<StubAssignment>? Rule,
    string? Path,
    string Message,
    EquatableArray<Diagnostic> Diagnostics
)
{
    /// <summary>Gets a value indicating whether the edit gave a rule that compiles.</summary>
    [MemberNotNullWhen(true, nameof(Rule))]
    public bool Succeeded => this.Rule is not null;

    /// <summary>Creates a refused edit.</summary>
    /// <param name="message">Why the edit was refused.</param>
    /// <param name="diagnostics">The diagnostics, or <see langword="null"/> for none.</param>
    /// <returns>The result.</returns>
    internal static RuleTreeEdit Refused(string message, EquatableArray<Diagnostic>? diagnostics = null)
    {
        return new RuleTreeEdit(null, null, message, diagnostics ?? EquatableArray<Diagnostic>.Empty);
    }
}
