namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// The parse of each library rule in one compile, shared by every scope of the compile: the parse of the rule text does
/// not depend on the registry a reference resolves against, so a library rule is parsed once however many references and
/// quantifier bodies use it.
/// </summary>
internal sealed class LibraryRuleParses
{
    private readonly Dictionary<string, (RuleDocumentNode Document, IReadOnlyList<Diagnostic> Diagnostics)> parses = [];

    /// <summary>Gets the parse of a library rule, and parses it the first time.</summary>
    /// <param name="rule">The library rule.</param>
    /// <param name="maxDepth">The maximum tree depth of the compile.</param>
    /// <returns>The rule as a document (one that declares no definitions when the text has none), and the parse diagnostics.</returns>
    public (RuleDocumentNode Document, IReadOnlyList<Diagnostic> Diagnostics) Get(LibraryRule rule, int maxDepth)
    {
        if (!this.parses.TryGetValue(rule.Name, out (RuleDocumentNode Document, IReadOnlyList<Diagnostic> Diagnostics) parse))
        {
            (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(rule.Text, maxDepth);
            parse = (root as RuleDocumentNode ?? new RuleDocumentNode([], root, root.Span), diagnostics);
            this.parses[rule.Name] = parse;
        }

        return parse;
    }
}
