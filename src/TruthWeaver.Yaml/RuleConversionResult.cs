namespace TruthWeaver.Yaml;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Diagnostics;

/// <summary>
/// The result of converting a rule from one format to another with <see cref="RuleFormatExtensions.Convert{TContext}"/>.
/// Like <c>CompilationResult</c>, it is returned, never thrown: a source that does not compile gives no text and the
/// compile diagnostics.
/// </summary>
/// <param name="Text">The rule in the target format, or <see langword="null"/> when the source did not compile.</param>
/// <param name="Diagnostics">Every diagnostic raised while compiling the source, including warnings on success.</param>
public sealed record RuleConversionResult(string? Text, EquatableArray<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether the conversion produced text (the source compiled with no error). When it is
    /// <see langword="true"/>, <see cref="Text"/> is not <see langword="null"/>.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Text))]
    public bool Succeeded => this.Text is not null;
}
