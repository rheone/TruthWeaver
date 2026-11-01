namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Expands the <c>rule("name")</c> references of one scope of a compile: the rule itself, or the body of a
/// <c>FORALL</c>/<c>EXISTS</c> quantifier, whose predicates resolve against the element registry of its collection.
/// <see cref="DefinitionExpander"/> holds the scope it is in and asks it for the scope of each quantifier body, so it
/// needs no context type.
/// </summary>
internal interface ILibraryRuleScope
{
    /// <summary>Expands one reference in this scope.</summary>
    /// <param name="reference">The reference.</param>
    /// <param name="libraryChain">The library rules being expanded through, outermost first.</param>
    /// <param name="site">The span of the outermost reference in the compiled text, or <see langword="null"/> when this reference is in the compiled text itself.</param>
    /// <param name="diagnostics">The diagnostics sink of the document that holds the reference.</param>
    /// <returns>
    /// The expanded library rule, or an <see cref="ErrorNode"/> when it cannot be expanded, and the number of nodes it
    /// puts in the compiled tree, for the node count of the caller.
    /// </returns>
    public (RuleNode Body, int NodeCount) Expand(
        LibraryReferenceNode reference,
        List<string> libraryChain,
        SourceSpan? site,
        List<Diagnostic> diagnostics
    );

    /// <summary>Gets the scope of the body of a quantifier in this scope.</summary>
    /// <param name="collectionName">The collection name of the quantifier.</param>
    /// <param name="variable">The variable of the quantifier, which the body can read as a data source.</param>
    /// <returns>
    /// The scope of the body, or this scope when no collection has the name: the build then reports the unknown
    /// collection and does not build the body.
    /// </returns>
    public ILibraryRuleScope EnterQuantifier(string collectionName, string variable);
}
