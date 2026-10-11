namespace TruthWeaver.Compilation;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;

/// <summary>
/// The result of <c>RuleCompiler.Compile</c> — never thrown, always returned (ADR-0002/ADR-0003).
/// <see cref="CompiledRule"/> is populated only when <see cref="Diagnostics"/> contains no
/// <see cref="DiagnosticSeverity.Error"/>-severity entries.
/// </summary>
/// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
/// <param name="CompiledRule">The compiled rule, or <see langword="null"/> if compilation failed.</param>
/// <param name="Diagnostics">Every diagnostic raised while parsing, validating, and analyzing.</param>
public sealed record CompilationResult<TContext>(CompiledRule<TContext>? CompiledRule, IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the compiled rule, or <see langword="null"/> if compilation failed. It is the same value as
    /// <see cref="CompiledRule"/>, and the compiler knows it is not <see langword="null"/> after a <see cref="Succeeded"/>
    /// check, so no <c>!</c> is needed.
    /// </summary>
    public CompiledRule<TContext>? Rule => this.CompiledRule;

    /// <summary>
    /// Gets a value indicating whether compilation succeeded (no <see cref="DiagnosticSeverity.Error"/> diagnostics). When
    /// it is <see langword="true"/>, <see cref="Rule"/> is not <see langword="null"/>.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Rule))]
    public bool Succeeded => this.CompiledRule is not null;

    /// <summary>Gets the compiled rule, or throws when compilation failed, for code where a rule that does not compile is a bug (a startup path, a test).</summary>
    /// <returns>The compiled rule.</returns>
    /// <exception cref="InvalidOperationException">Compilation produced no rule. The message lists each <see cref="DiagnosticSeverity.Error"/> diagnostic as <see cref="FormatDiagnostics"/> renders it, without rule text.</exception>
    public CompiledRule<TContext> GetRuleOrThrow()
    {
        if (this.CompiledRule is { } rule)
        {
            return rule;
        }

        // Only errors explain a missing rule; warnings and info findings would bury them.
        IEnumerable<string> errors = this
            .Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => DiagnosticFormatter.Format(d));
        throw new InvalidOperationException("Compilation produced no rule:\n" + string.Join("\n", errors));
    }

    /// <summary>
    /// Renders <see cref="Diagnostics"/> as plain text for a log or an editor panel (see <see cref="DiagnosticFormatter"/>).
    /// The structured members of each <see cref="Diagnostic"/> remain available for callers that lay them out themselves.
    /// </summary>
    /// <param name="source">The rule text that was compiled, so locations show a line and column and the offending line; <see langword="null"/> to show offsets only.</param>
    /// <returns>The rendered diagnostics, one block each; empty when there are none.</returns>
    public string FormatDiagnostics(string? source = null)
    {
        return DiagnosticFormatter.Format(this.Diagnostics, source);
    }
}
