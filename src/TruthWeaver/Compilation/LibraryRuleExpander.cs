namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;

/// <summary>
/// Expands the <c>rule("name")</c> references of one compile against <see cref="CompilerOptions.Library"/>, with the same
/// machinery as <c>let</c> definitions: a reference is replaced by the library rule's own expanded tree before
/// <see cref="RuleNodeCompiler{TContext}"/> builds the compiled tree.
/// </summary>
/// <typeparam name="TContext">The application context type of the compile.</typeparam>
/// <param name="registry">The predicate registry of the compile, which a library rule's predicate names resolve against.</param>
/// <param name="options">The compiler options of the compile, which hold the library and its data source names.</param>
internal sealed class LibraryRuleExpander<TContext>(PredicateRegistry<TContext> registry, CompilerOptions options)
{
    // The expansion of each library rule for each site, or null when it failed; keyed by the rule name as the library
    // holds it, so the case of the reference does not matter.
    private readonly Dictionary<(string Name, SourceSpan Site), RuleNode?> expanded = [];

    /// <summary>Expands one reference.</summary>
    /// <param name="reference">The reference.</param>
    /// <param name="libraryChain">The library rules being expanded through, outermost first.</param>
    /// <param name="site">The span of the outermost reference in the compiled text, or <see langword="null"/> when this reference is in the compiled text itself.</param>
    /// <param name="diagnostics">The diagnostics sink of the document that holds the reference.</param>
    /// <returns>The expanded library rule, or an <see cref="ErrorNode"/> when it cannot be expanded.</returns>
    public RuleNode Expand(
        LibraryReferenceNode reference,
        List<string> libraryChain,
        SourceSpan? site,
        List<Diagnostic> diagnostics
    )
    {
        SourceSpan span = site ?? reference.Span;
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
            return new ErrorNode(span);
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
            return new ErrorNode(span);
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
            return new ErrorNode(span);
        }

        // A library rule takes no arguments, so its expansion for one site is the same wherever the site references it.
        // Without this, a chain of library rules that each reference the one below many times is expanded once for each
        // path through the chain, which grows exponentially with the depth of the chain. A failed expansion has already
        // reported its errors, so a later reference to it reports nothing more.
        if (!this.expanded.TryGetValue((rule.Name, span), out RuleNode? body))
        {
            body = this.ExpandRule(rule, [.. libraryChain, rule.Name], span, diagnostics);
            this.expanded[(rule.Name, span)] = body;
        }

        return body ?? new ErrorNode(span);
    }

    // A library rule code already names the library rule it is about, so it passes through as it is: that keeps one
    // diagnostic for one cycle or one missing rule, however deep the reference is. Any other error is about the text of this
    // library rule, so it is reported under InvalidLibraryRule, with the name of the rule. Warnings and information are the
    // library rule's own concern and are dropped.
    private static Diagnostic AsCallerDiagnostic(LibraryRule rule, Diagnostic error, SourceSpan span)
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

        return Diagnostic.Error(
            DiagnosticCodes.InvalidLibraryRule,
            $"Library rule '{rule.Name}' does not compile here: {error.Code} {error.Message}",
            span,
            expected: error.Expected,
            found: error.Found,
            suggestion: error.Suggestion is { Kind: DiagnosticSuggestionKind.Hint } hint ? hint : null
        );
    }

    // Adds the errors of one step of a library rule to the caller's diagnostics, and says whether there were any.
    private static bool ReportErrors(
        LibraryRule rule,
        IReadOnlyList<Diagnostic> found,
        SourceSpan span,
        List<Diagnostic> diagnostics
    )
    {
        int before = diagnostics.Count;
        diagnostics.AddRange(
            found.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => AsCallerDiagnostic(rule, d, span))
        );
        return diagnostics.Count > before;
    }

    // Parses, expands and checks one library rule, or reports its errors and returns null.
    private RuleNode? ExpandRule(LibraryRule rule, List<string> libraryChain, SourceSpan span, List<Diagnostic> diagnostics)
    {
        (RuleNode parsed, IReadOnlyList<Diagnostic> parseDiagnostics) = DslParser.Parse(rule.Text, options.MaxDepth);
        if (ReportErrors(rule, parseDiagnostics, span, diagnostics))
        {
            return null;
        }

        RuleDocumentNode document = parsed as RuleDocumentNode ?? new RuleDocumentNode([], parsed, parsed.Span);
        (RuleNode body, IReadOnlyList<Diagnostic> expansionDiagnostics) = DefinitionExpander.Expand(
            document,
            registry,
            options,
            this,
            libraryChain,
            span
        );
        if (ReportErrors(rule, expansionDiagnostics, span, diagnostics))
        {
            return null;
        }

        // The library rule's predicate and data source names resolve against this compile. Building the expanded library
        // rule on its own finds a name the caller does not hold, and the diagnostic can then name the library rule. The
        // compile of the caller builds the same nodes again, and so reports no second error for them.
        (_, _, IReadOnlyList<Diagnostic> buildDiagnostics, _) = RuleNodeCompiler<TContext>.Compile(body, registry, options);
        return ReportErrors(rule, buildDiagnostics, span, diagnostics) ? null : body;
    }

    // A hint, not a replacement: the span covers the whole reference, so the bare name is not text to put there.
    private DiagnosticSuggestion? NearestNameHint(string name)
    {
        return options.Library is not null && NameSuggester.Nearest(name, options.Library.Names) is { } nearest
            ? new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, $"Did you mean the library rule '{nearest}'?")
            : null;
    }
}
