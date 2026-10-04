namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// The locations of the two parts of a variable reference. A DSL reference carries spans; a JSON or YAML one carries the
/// paths of its <c>from</c> and <c>query</c> members (JSON also gets spans later, from those paths).
/// </summary>
/// <param name="SourceSpan">The span of the source name.</param>
/// <param name="QuerySpan">The span of the query string.</param>
/// <param name="SourcePath">The tree path of the <c>from</c> member, or <see langword="null"/> for the DSL.</param>
/// <param name="QueryPath">The tree path of the <c>query</c> member, or <see langword="null"/> for the DSL.</param>
internal sealed record VariableParts(
    SourceSpan SourceSpan,
    SourceSpan QuerySpan,
    string? SourcePath = null,
    string? QueryPath = null
);
