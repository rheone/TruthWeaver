namespace TruthWeaver.Tool.Core.Diagnostics;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Loading;

/// <summary>
/// The diagnostics of one input, with the text that their spans point into, for <see cref="DiagnosticWriter"/>. The
/// writers turn a span into a line and a column with <see cref="Text"/>.
/// </summary>
/// <param name="Origin">The input: a file path, <c>-</c> for standard input, or a name.</param>
/// <param name="Text">The text that the spans point into, or <see langword="null"/> when there is none (the input could not be read).</param>
/// <param name="RulePath">
/// The path of an embedded rule inside the input, such as <c>$.rules.publish</c>, or <see langword="null"/> when the whole
/// input is the rule. The spans of an embedded rule point into the rule, not into the input, so the SARIF writer gives
/// such a diagnostic a logical location and no region in the file.
/// </param>
/// <param name="Diagnostics">The diagnostics.</param>
public sealed record DiagnosticDocument(string Origin, string? Text, string? RulePath, EquatableArray<Diagnostic> Diagnostics)
{
    /// <summary>Creates the document of a loaded rule.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="diagnostics">The diagnostics of the rule.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static DiagnosticDocument For(LoadedRule rule, IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new DiagnosticDocument(rule.Source.Origin, rule.Text, rule.Path, new EquatableArray<Diagnostic>(diagnostics));
    }
}
