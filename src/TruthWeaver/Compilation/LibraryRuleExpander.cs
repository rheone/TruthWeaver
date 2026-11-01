namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;

/// <summary>
/// Expands the <c>rule("name")</c> references of one scope of a compile against <see cref="CompilerOptions.Library"/>,
/// with the same machinery as <c>let</c> definitions: a reference is replaced by the library rule's own expanded tree
/// before <see cref="RuleNodeCompiler{TContext}"/> builds the compiled tree. The scope is the rule itself, or the body of
/// a quantifier, where <typeparamref name="TContext"/> is the element of the collection.
/// </summary>
/// <typeparam name="TContext">The context type of the scope.</typeparam>
/// <param name="registry">The predicate registry of the scope, which a library rule's predicate names resolve against.</param>
/// <param name="options">
/// The compiler options of the scope, which hold the library and its data source names: in a quantifier body, the
/// variable of the quantifier is one of them when the collection projects its elements to a data source.
/// </param>
/// <param name="parses">The parse of each library rule, shared by every scope of the compile.</param>
internal sealed class LibraryRuleExpander<TContext>(
    PredicateRegistry<TContext> registry,
    CompilerOptions options,
    LibraryRuleParses parses
) : ILibraryRuleScope
{
    // The expansion of each reference for each site, or null when it failed. The pin is part of the key, so a pinned and
    // an unpinned reference at one site are each checked. The name and the pin are upper-cased in the key, because the
    // library and the pin check compare them without case.
    private readonly Dictionary<(string Name, string? Pin, SourceSpan Site), (RuleNode? Body, int NodeCount)> expanded = [];

    // The node count of each library rule that expanded and compiled without an error in this scope.
    private readonly Dictionary<string, int> checkedNodeCounts = [];

    // The scope of each quantifier body in this scope, by the upper-cased collection name and the variable.
    private readonly Dictionary<(string Collection, string Variable), ILibraryRuleScope> bodyScopes = [];

    /// <inheritdoc/>
    public (RuleNode Body, int NodeCount) Expand(
        LibraryReferenceNode reference,
        List<string> libraryChain,
        SourceSpan? site,
        List<Diagnostic> diagnostics
    )
    {
        SourceSpan span = site ?? reference.Span;

        // A library rule takes no arguments, so its expansion for one site is the same wherever the site references it.
        // Without this, a chain of library rules that each reference the one below many times is expanded once for each
        // path through the chain, which grows exponentially with the depth of the chain. A failed reference has already
        // reported its errors, so a later reference to it at the same site reports nothing more: that holds for an
        // unknown rule, a stale pin and a cycle as for an error in the library rule's text. The uses of a definition share
        // the site of a reference in its body, so the cache is consulted before any check.
        (string, string?, SourceSpan) key = (reference.Name.ToUpperInvariant(), reference.PinnedHash?.ToUpperInvariant(), span);
        if (!this.expanded.TryGetValue(key, out (RuleNode? Body, int NodeCount) expansion))
        {
            expansion = this.Resolve(reference, libraryChain, span, diagnostics);
            this.expanded[key] = expansion;
        }

        return expansion.Body is { } body ? (body, expansion.NodeCount) : (new ErrorNode(span), 1);
    }

    /// <inheritdoc/>
    public ILibraryRuleScope EnterQuantifier(string collectionName, string variable)
    {
        if (!registry.TryGetCollection(collectionName, out ICollectionBinding<TContext>? binding))
        {
            return this;
        }

        // The collection lookup ignores case, so the key does too. A variable is case-sensitive, like a data source name.
        (string, string) key = (collectionName.ToUpperInvariant(), variable);
        if (!this.bodyScopes.TryGetValue(key, out ILibraryRuleScope? scope))
        {
            scope = binding.CreateLibraryScope(variable, options, parses);
            this.bodyScopes[key] = scope;
        }

        return scope;
    }

    // A library rule code already names the library rule it is about, so it passes through as it is: that keeps one
    // diagnostic for one cycle or one missing rule, however deep the reference is. Any other error is about the text of this
    // library rule, so it is reported under InvalidLibraryRule, with the name of the rule and, when the error is located in
    // the text of the rule, the locator of that place.
    private static Diagnostic AsCallerDiagnostic(LibraryRule rule, Diagnostic error, SourceSpan span, bool locateInRule)
    {
        if (
            error.Code
            is DiagnosticCodes.UnknownLibraryRule
                or DiagnosticCodes.LibraryRuleHashMismatch
                or DiagnosticCodes.LibraryRuleCycleDetected
                or DiagnosticCodes.InvalidLibraryRule
        )
        {
            return error with { Span = span };
        }

        string message = locateInRule
            ? LibraryLocator.Of(rule.Name, rule.Text, error.Span).AppendTo(error.Message)
            : error.Message;
        return Diagnostic.Error(
            DiagnosticCodes.InvalidLibraryRule,
            $"Library rule '{rule.Name}' does not compile here: {error.Code} {message}",
            span,
            expected: error.Expected,
            found: error.Found,
            suggestion: error.Suggestion is { Kind: DiagnosticSuggestionKind.Hint } hint ? hint : null
        );
    }

    // Adds what one step of a library rule raised to the caller's diagnostics, and says whether it raised an error. The
    // parse and the expansion report spans in the text of the library rule, which locateInRule turns into a locator; the
    // build adds the locator of the node itself. A warning or information of the parse or the expansion keeps its code and
    // severity, and moves to the reference with the locator in its message. The build passes none on: the build of the
    // caller builds the same nodes and reports them there, once for each reference site.
    private bool ReportErrors(
        LibraryRule rule,
        IReadOnlyList<Diagnostic> found,
        SourceSpan span,
        List<Diagnostic> diagnostics,
        bool locateInRule
    )
    {
        bool failed = false;
        foreach (Diagnostic raised in found)
        {
            if (raised.Severity == DiagnosticSeverity.Error)
            {
                diagnostics.Add(AsCallerDiagnostic(rule, raised, span, locateInRule));
                failed = true;
            }
            else if (parses.Surfaced.Contains(raised))
            {
                // Already passed on, located, by a nested library rule at this same site.
                diagnostics.Add(raised);
            }
            else if (locateInRule)
            {
                Diagnostic surfaced = raised with
                {
                    Span = span,
                    Message = LibraryLocator.Of(rule.Name, rule.Text, raised.Span).AppendTo(raised.Message),
                    Path = null,
                    Suggestion = raised.Suggestion is { Kind: DiagnosticSuggestionKind.Hint } hint ? hint : null,
                    EnclosedBy = null,
                };
                parses.Surfaced.Add(surfaced);
                diagnostics.Add(surfaced);
            }
        }

        return failed;
    }

    // Checks one reference that is not in the cache and expands the library rule it names, or reports why it cannot and
    // returns null.
    private (RuleNode? Body, int NodeCount) Resolve(
        LibraryReferenceNode reference,
        List<string> libraryChain,
        SourceSpan span,
        List<Diagnostic> diagnostics
    )
    {
        if (options.Library is null || !options.Library.TryGet(reference.Name, out LibraryRule? rule))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.UnknownLibraryRule,
                    $"'{reference.Name}' is not a rule in the compiler's rule library.",
                    span,
                    expected: "the name of a rule in the compiler's rule library",
                    found: $"'{reference.Name}'",
                    suggestion: this.NearestNameHint(reference.Name)
                )
            );
            return (null, 0);
        }

        // A hash is hexadecimal digits, so the pin matches without case.
        if (reference.PinnedHash is { } pin && !string.Equals(pin, rule.Hash, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.LibraryRuleHashMismatch,
                    $"Library rule '{rule.Name}' has content hash {rule.Hash}, not the pinned hash {pin}.",
                    span,
                    expected: $"the current content hash {rule.Hash}",
                    found: $"the pinned hash {pin}",
                    suggestion: new DiagnosticSuggestion(
                        DiagnosticSuggestionKind.Hint,
                        "Check the change to the library rule, then update the pin or remove it."
                    )
                )
            );
            return (null, 0);
        }

        int cycleStart = libraryChain.FindIndex(name => string.Equals(name, rule.Name, StringComparison.OrdinalIgnoreCase));
        if (cycleStart >= 0)
        {
            string path = string.Join(" -> ", [.. libraryChain.Skip(cycleStart), rule.Name]);
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.LibraryRuleCycleDetected,
                    $"Library rule cycle: {path}.",
                    span,
                    expected: "no library rule that references itself through a chain of library rules",
                    found: $"the cycle {path}"
                )
            );
            return (null, 0);
        }

        return this.ExpandRule(rule, [.. libraryChain, rule.Name], span, diagnostics);
    }

    // Expands one library rule for one site and checks it, or reports its errors and returns null. The parse is shared by
    // every scope of the compile and the check by every site in this scope; only the expansion, which puts the site span
    // on every node, is per site.
    private (RuleNode? Body, int NodeCount) ExpandRule(
        LibraryRule rule,
        List<string> libraryChain,
        SourceSpan span,
        List<Diagnostic> diagnostics
    )
    {
        (RuleDocumentNode Document, IReadOnlyList<Diagnostic> Diagnostics) parse = parses.Get(rule, options.MaxDepth);
        if (this.ReportErrors(rule, parse.Diagnostics, span, diagnostics, locateInRule: true))
        {
            return (null, 0);
        }

        (RuleNode body, IReadOnlyList<Diagnostic> expansionDiagnostics) = DefinitionExpander.Expand(
            parse.Document,
            registry,
            options,
            this,
            libraryChain,
            rule,
            span
        );
        if (this.ReportErrors(rule, expansionDiagnostics, span, diagnostics, locateInRule: true))
        {
            return (null, 0);
        }

        // A library rule that expanded cleanly once expands cleanly at every site: a cycle through it would have failed it
        // at the first site too. Its build differs only in spans, so it is checked once. A failed rule is not kept here,
        // so each site reports its own errors, with the cycle path seen from that site.
        if (this.checkedNodeCounts.TryGetValue(rule.Name, out int nodeCount))
        {
            return (body, nodeCount);
        }

        // The library rule's predicate and data source names resolve against this scope. Building the expanded library
        // rule on its own finds a name the caller does not hold, and the diagnostic can then name the library rule. The
        // compile of the caller builds the same nodes again, and so reports no second error for them. The node count of
        // the build is what each reference adds to the node count of the caller.
        (_, _, IReadOnlyList<Diagnostic> buildDiagnostics, nodeCount) = RuleNodeCompiler<TContext>.Compile(
            body,
            registry,
            options
        );
        if (this.ReportErrors(rule, buildDiagnostics, span, diagnostics, locateInRule: false))
        {
            return (null, 0);
        }

        this.checkedNodeCounts[rule.Name] = nodeCount;
        return (body, nodeCount);
    }

    // A hint, not a replacement: the span covers the whole reference, so the bare name is not text to put there.
    private DiagnosticSuggestion? NearestNameHint(string name)
    {
        return options.Library is not null && NameSuggester.Nearest(name, options.Library.Names) is { } nearest
            ? new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, $"Did you mean the library rule '{nearest}'?")
            : null;
    }
}
