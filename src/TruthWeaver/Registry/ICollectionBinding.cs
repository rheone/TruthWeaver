namespace TruthWeaver.Registry;

using Microsoft.Extensions.Logging;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;

/// <summary>
/// The non-generic-over-<c>TItem</c> face of a <see cref="CollectionBinding{TContext, TItem}"/>
/// (ADR-0010 decision 3), the shape <see cref="PredicateRegistry{TContext}"/> stores a collection binding as so
/// that neither the registry nor <c>RuleNodeCompiler&lt;TContext&gt;</c>/<c>Evaluator&lt;TContext&gt;</c> need to
/// know the element type. Only the binding itself (created inside the generic
/// <see cref="PredicateRegistryBuilder{TContext}.AddCollection{TItem}"/> call, where <c>TItem</c> is known) does.
/// </summary>
/// <typeparam name="TContext">The outer context type the collection is selected from.</typeparam>
internal interface ICollectionBinding<TContext>
{
    /// <summary>Gets the collection's registered name.</summary>
    public string Name { get; }

    /// <summary>
    /// Validates and builds <paramref name="body"/> against this collection's element registry. The quantifier
    /// body's own compile step, run in the element's own scope rather than the outer <c>RuleNodeCompiler</c>'s.
    /// </summary>
    /// <param name="variable">
    /// The quantifier's bound variable name. When this collection was registered with an element-to-data-source
    /// projection (ADR-0010 decision 3), the name is declared to the body's own <see cref="CompilerOptions.DataSources"/>
    /// so a body term may read the element untyped with <c>from(variable, "...")</c>, alongside typed element
    /// predicates. A collection registered without a projection leaves the name undeclared, so such a read is the
    /// same <see cref="DiagnosticCodes.UndeclaredDataSource"/> diagnostic ADR-0006 already gives any other
    /// undeclared source name.
    /// </param>
    /// <param name="body">The quantifier's body, as a raw parse node.</param>
    /// <param name="options">The compiler's resource limits and mode.</param>
    /// <param name="depth">The structural tree depth the body starts at (the quantifier node's own depth plus one).</param>
    /// <param name="quantifierDepth">How many quantifiers deep the body itself starts at (the quantifier node's own depth), for <see cref="CompilerOptions.MaxQuantifierDepth"/> across the collection-binding boundary.</param>
    /// <param name="enclosingVariables">The bound variable names already in scope from every enclosing quantifier, innermost last, for the shadowed-variable rule across the collection-binding boundary.</param>
    /// <param name="startNodeCount">
    /// How many nodes the enclosing compile has already counted against <see cref="CompilerOptions.MaxNodeCount"/>
    /// (the quantifier node itself included), so the body's nodes extend that same budget instead of starting a
    /// fresh one (ADR-0010 line 175: "a quantifier counts as one node plus its body, once").
    /// </param>
    /// <returns>
    /// The built body tree (or <see langword="null"/> on an error), every diagnostic raised building it, and the
    /// running node count after compiling the body (<paramref name="startNodeCount"/> plus however many nodes the
    /// body itself counted), for the caller to resume counting from.
    /// </returns>
    public (Expression? Tree, IReadOnlyList<Diagnostic> Diagnostics, int NodeCount) CompileBody(
        string variable,
        RuleNode body,
        CompilerOptions options,
        int depth,
        int quantifierDepth,
        IReadOnlyList<string> enclosingVariables,
        int startNodeCount
    );

    /// <summary>
    /// Evaluates a <c>FORALL</c>/<c>EXISTS</c> quantifier over this collection (ADR-0010 decisions 4, 5 and 6):
    /// selects and materializes the elements once, folds <paramref name="body"/> over them with the quantifier's
    /// Strong Kleene semantics and short-circuiting, and records one trace child per element.
    /// </summary>
    /// <param name="context">The outer context to select the collection from.</param>
    /// <param name="kind">Whether this is <c>FORALL</c> or <c>EXISTS</c>.</param>
    /// <param name="variable">The bound variable name, for the trace labels.</param>
    /// <param name="body">The already-compiled body, shared by every element.</param>
    /// <param name="options">The evaluation options, including <see cref="EvaluationOptions.MaxElementEvaluations"/>.</param>
    /// <param name="state">The whole evaluation's shared fault list, trace log, abort flag and memo tables.</param>
    /// <param name="services">The service provider for class-based element predicates.</param>
    /// <param name="logger">The logger faults inside an element are written to.</param>
    /// <param name="dataSources">The data sources supplied to this evaluation.</param>
    /// <param name="outerScope">The enclosing display scope path (built from bound variable names), or <see langword="null"/> outside any quantifier. Labels <see cref="Fault.Scope"/> and the flat trace log (ADR-0010 decision 7).</param>
    /// <param name="outerMemoScope">The enclosing memoization scope path (built from registered collection names), or <see langword="null"/> outside any quantifier (ADR-0010 decision 5). Different from <paramref name="outerScope"/>: it is what the selector-materialization cache and a nested evaluator's term memo key are built from.</param>
    /// <param name="cancellationToken">Cancels the evaluation.</param>
    /// <returns>The quantifier's value and its trace node (one child per element).</returns>
    public ValueTask<(TruthValue Value, TraceNode Node)> EvaluateQuantifierAsync(
        TContext context,
        QuantifierKind kind,
        string variable,
        Expression body,
        EvaluationOptions options,
        EvaluationState state,
        IServiceProvider services,
        ILogger logger,
        DataSources? dataSources,
        string? outerScope,
        string? outerMemoScope,
        CancellationToken cancellationToken
    );
}
