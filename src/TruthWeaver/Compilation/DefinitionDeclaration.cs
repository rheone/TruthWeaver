namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;

/// <summary>
/// One <c>let</c> definition of a rule document, as <see cref="RuleDefinitions"/> reads it: its name, its parameters and
/// where it is written. The body is in <see cref="RuleDefinitions.PrintJson"/> at <see cref="Path"/> followed by
/// <c>.body</c>.
/// </summary>
/// <param name="Name">The name of the definition, as declared.</param>
/// <param name="Parameters">The parameter names in declaration order; empty for a definition without parameters.</param>
/// <param name="Path">
/// The path of the definition in the JSON tree format of the document: <c>$.definitions.</c> followed by the name, such as
/// <c>$.definitions.allApprovals</c>. It is the same path for rule text, JSON and YAML.
/// </param>
/// <param name="Span">
/// The span of the whole declaration in the source text: from <c>let</c> to the closing <c>;</c> in rule text, or the
/// <c>definitions</c> entry in JSON and YAML.
/// </param>
/// <param name="NameSpan">The span of the name of the definition in the source text, or the span of the entry for JSON.</param>
public sealed record DefinitionDeclaration(
    string Name,
    EquatableArray<string> Parameters,
    string Path,
    SourceSpan Span,
    SourceSpan NameSpan
);
