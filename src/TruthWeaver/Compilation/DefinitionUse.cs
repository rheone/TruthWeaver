namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;

/// <summary>
/// One use of a <c>let</c> definition in a rule document, as <see cref="RuleDefinitions"/> reads it: the name it applies,
/// its arguments, where it is written and where its expansion sits in the compiled rule.
/// </summary>
/// <param name="Name">The name of the definition, as written at the use.</param>
/// <param name="ArgumentCount">The number of arguments the use supplies; 0 for a use without arguments.</param>
/// <param name="Definition">
/// The name of the definition whose body holds the use, or <see langword="null"/> for a use in the rule itself (an
/// argument of a use belongs to the place of that use).
/// </param>
/// <param name="Path">
/// The path of the use in the JSON tree format of the document (<see cref="RuleDefinitions.PrintJson"/>), such as
/// <c>$.operands[1]</c>. Argument <c>i</c> is at this path followed by <c>.operands[i]</c>.
/// </param>
/// <param name="ExpandedPaths">
/// The paths in the compiled rule (the form of <c>CompiledRule.PrintJson</c> and <c>CompiledRule.Notes</c>) where the
/// expansion of this use sits, in tree order. A use in the rule has one path; a use inside a definition body has one
/// path for each place the definition expands; a use that never expands (an argument that the body does not read, or a
/// use that does not compile) has none.
/// </param>
/// <param name="Span">The span of the whole use in the source text, from the name to the closing <c>)</c>.</param>
/// <param name="NameSpan">The span of the name of the use in the source text, or the span of the <c>name</c> value for JSON.</param>
public sealed record DefinitionUse(
    string Name,
    int ArgumentCount,
    string? Definition,
    string Path,
    EquatableArray<string> ExpandedPaths,
    SourceSpan Span,
    SourceSpan NameSpan
);
