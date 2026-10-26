namespace TruthWeaver.Registry;

using Microsoft.Extensions.Logging;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;

/// <summary>
/// A typed collection binding (ADR-0010 decision 3): a registered name, a selector from
/// <typeparamref name="TContext"/> to the collection's elements, and the element registry
/// (<c>PredicateRegistry&lt;Element&lt;TContext, TItem&gt;&gt;</c>) a quantifier's body resolves against. Created
/// by <see cref="PredicateRegistryBuilder{TContext}.AddCollection{TItem}"/>, where <typeparamref name="TItem"/> is
/// known; stored on <see cref="PredicateRegistry{TContext}"/> through the non-generic
/// <see cref="ICollectionBinding{TContext}"/> face, so neither the registry, the compiler nor the evaluator need
/// to be generic over the element type.
/// </summary>
/// <typeparam name="TContext">The outer context type the collection is selected from.</typeparam>
/// <typeparam name="TItem">The collection's element type.</typeparam>
/// <param name="name">The collection's registered name.</param>
/// <param name="selector">Reads the collection's elements from the outer context.</param>
/// <param name="elementRegistry">The element predicates a quantifier's body resolves against.</param>
/// <param name="elementSource">
/// Projects one element to an <see cref="IDataSource"/> scoped to it, enabling the untyped element read (ADR-0010
/// decision 3): a body may then write <c>from(variable, "...")</c> as well as typed element predicates.
/// <see langword="null"/> (the default) when the collection supports only the typed path; a body that still writes
/// <c>from(variable, ...)</c> against such a collection gets the ordinary undeclared-source diagnostic.
/// </param>
internal sealed class CollectionBinding<TContext, TItem>(
    string name,
    Func<TContext, IEnumerable<TItem>?> selector,
    PredicateRegistry<Element<TContext, TItem>> elementRegistry,
    Func<TItem, IDataSource>? elementSource = null
) : ICollectionBinding<TContext>
{
    public string Name => name;

    public (Expression? Tree, IReadOnlyList<Diagnostic> Diagnostics, int NodeCount) CompileBody(
        string variable,
        RuleNode body,
        CompilerOptions options,
        int depth,
        int quantifierDepth,
        IReadOnlyList<string> enclosingVariables,
        int startNodeCount
    )
    {
        CompilerOptions bodyOptions = elementSource is null ? options : DeclareElementSource(options, variable);
        (Expression? tree, RuleNode _, IReadOnlyList<Diagnostic> diagnostics, int nodeCount) = RuleNodeCompiler<
            Element<TContext, TItem>
        >.Compile(body, elementRegistry, bodyOptions, depth, quantifierDepth, enclosingVariables, startNodeCount);
        return (tree, diagnostics, nodeCount);
    }

    public async ValueTask<(TruthValue Value, TraceNode Node)> EvaluateQuantifierAsync(
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
    )
    {
        string label = QuantifierText.Label(kind);
        string description = $"{label}({variable} IN {name})";

        void AddFlat(TruthValue? value, bool notEvaluated)
        {
            state.AddTrace(
                new TraceEntry(outerScope is null ? description : $"{outerScope}: {description}", value, notEvaluated)
            );
        }

        // ADR-0010 decision 5: the selector is called at most once per (binding, outer element) within one
        // evaluation. The cache key is the enclosing memoization scope (the outer element) plus this collection's
        // registered name (the binding); a second quantifier over the same binding at the same outer element, even
        // under a different bound variable, reuses the first call's outcome instead of invoking the selector again.
        (string? OuterScope, string Binding) materializationKey = (outerMemoScope, name);
        bool firstMaterialization = !state.CollectionMaterializations.TryGetValue(
            materializationKey,
            out CollectionMaterialization materialization
        );
        if (firstMaterialization)
        {
            try
            {
                materialization = new CollectionMaterialization(false, null, selector(context)?.ToList());
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                materialization = new CollectionMaterialization(true, ex, null);
            }

            state.CollectionMaterializations[materializationKey] = materialization;
        }

        if (materialization.Faulted)
        {
            // ADR-0010 decision 4: a throwing selector is Unknown plus one fault for the collection itself. The
            // fault is recorded only the first time the selector actually ran, matching how a cached term memo
            // hit never re-raises the fault that produced it.
            if (firstMaterialization)
            {
                FaultRecorder.Record(state, logger, options, outerScope, new TermIdentity(name, []), materialization.Fault!);
            }

            AddFlat(TruthValue.Unknown, false);
            return (TruthValue.Unknown, new TraceNode(description, TruthValue.Unknown, false, EquatableArray<TraceNode>.Empty));
        }

        IReadOnlyList<TItem>? elements = (IReadOnlyList<TItem>?)materialization.Elements;
        if (elements is null)
        {
            // ADR-0010 decision 4: a null collection is Unknown with no fault (the catalog's default NullBehavior).
            AddFlat(TruthValue.Unknown, false);
            return (
                TruthValue.Unknown,
                new TraceNode($"{description} [no collection]", TruthValue.Unknown, false, EquatableArray<TraceNode>.Empty)
            );
        }

        if (elements.Count == 0)
        {
            // ADR-0010 decision 4: the identity elements of AND/OR.
            TruthValue identityValue = kind == QuantifierKind.ForAll ? TruthValue.True : TruthValue.False;
            AddFlat(identityValue, false);
            return (identityValue, new TraceNode(description, identityValue, false, EquatableArray<TraceNode>.Empty));
        }

        TruthValue accumulator = kind == QuantifierKind.ForAll ? TruthValue.True : TruthValue.False;
        bool exhaustive = options.Mode == EvaluationMode.Exhaustive;
        bool stop = false;

        // Set only when THIS quantifier's own loop hits MaxElementEvaluations (ADR-0010 decision 6): the elements
        // this quantifier never reached could still have decided it, so its result is forced Unknown regardless of
        // what the elements reached so far folded to. A fault-budget abort instead folds each skipped element as
        // Unknown, so an element already at the stop value keeps the quantifier definite.
        bool elementLimitExceeded = false;
        List<TraceNode> children = [];
        for (int i = 0; i < elements.Count; i++)
        {
            string elementLabel = $"{variable}[{i}]";
            string elementScope = outerScope is null ? elementLabel : $"{outerScope}/{elementLabel}";

            // ADR-0010 decision 5: the memo scope chains on the collection's registered name, not the bound
            // variable, so two quantifiers over the same binding under different variable names share memo
            // entries for an equal term, while the same variable name reused over a different collection does not.
            string bindingLabel = $"{name}[{i}]";
            string elementMemoScope = outerMemoScope is null ? bindingLabel : $"{outerMemoScope}/{bindingLabel}";

            if (stop || state.Aborted)
            {
                TraceNode skippedBody = new(TraceText.Of(body), null, true, EquatableArray<TraceNode>.Empty);
                children.Add(new TraceNode(elementLabel, null, true, new EquatableArray<TraceNode>([skippedBody])));

                // An element skipped by an abort could still decide the quantifier, so it counts as Unknown, like an
                // operand an AND/OR chain skips on abort. A short-circuit skip needs no fold: the accumulator already
                // holds the stop value, which Unknown cannot change.
                if (!stop)
                {
                    accumulator =
                        kind == QuantifierKind.ForAll
                            ? K3Logic.And(accumulator, TruthValue.Unknown)
                            : K3Logic.Or(accumulator, TruthValue.Unknown);
                }

                continue;
            }

            if (state.ElementEvaluations >= options.MaxElementEvaluations)
            {
                // ADR-0010 decision 6: over the bound, the current quantifier is Unknown with one fault and the
                // whole evaluation aborts, the same way the fault budget aborts - unconditionally here, so it
                // still aborts even when the fault budget itself has not been reached.
                FaultRecorder.Record(
                    state,
                    logger,
                    options,
                    outerScope,
                    new TermIdentity(name, []),
                    new ElementEvaluationLimitExceededException(options.MaxElementEvaluations)
                );
                state.Aborted = true;
                elementLimitExceeded = true;
                TraceNode skippedBody = new(TraceText.Of(body), null, true, EquatableArray<TraceNode>.Empty);
                children.Add(new TraceNode(elementLabel, null, true, new EquatableArray<TraceNode>([skippedBody])));
                continue;
            }

            state.ElementEvaluations++;
            Element<TContext, TItem> elementContext = new(context, elements[i], i);
            DataSources? elementDataSources = elementSource is null
                ? dataSources
                : WithElementSource(dataSources, variable, elements[i], elementSource);
            Evaluator<Element<TContext, TItem>> nested = new(
                elementContext,
                services,
                elementRegistry,
                options,
                cancellationToken,
                logger,
                elementDataSources,
                state,
                elementScope,
                elementMemoScope
            );
            (TruthValue value, TraceNode node) = await nested.EvaluateSharedAsync(body).ConfigureAwait(false);
            accumulator = kind == QuantifierKind.ForAll ? K3Logic.And(accumulator, value) : K3Logic.Or(accumulator, value);
            children.Add(new TraceNode(elementLabel, value, false, new EquatableArray<TraceNode>([node])));

            bool elementStops = kind == QuantifierKind.ForAll ? value == TruthValue.False : value == TruthValue.True;
            if (!exhaustive && elementStops)
            {
                stop = true;
            }
        }

        if (elementLimitExceeded)
        {
            accumulator = TruthValue.Unknown;
        }

        AddFlat(accumulator, false);
        return (accumulator, new TraceNode(description, accumulator, false, new EquatableArray<TraceNode>(children)));
    }

    /// <summary>
    /// Declares <paramref name="variable"/> as a data source name for the body's own compile pass (ADR-0010
    /// decision 3), copying every name already declared on <paramref name="options"/> (keeping each one's
    /// validator, if any) so the body still sees every outer-declared source besides its own element binding.
    /// </summary>
    private static CompilerOptions DeclareElementSource(CompilerOptions options, string variable)
    {
        DataSourceDeclarations declared = [];
        if (options.DataSources is { } outer)
        {
            foreach (string outerName in outer)
            {
                if (outer[outerName] is { } validator)
                {
                    declared.Add(outerName, validator);
                }
                else
                {
                    declared.Add(outerName);
                }
            }
        }

        // A bare declaration never overwrites a validator the name already carries (DataSourceDeclarations.Add's
        // own contract), so a variable name that happens to shadow an outer declared source keeps that source's
        // validator; only an undeclared variable name gains one here, with no validator (the query is the body's
        // own JSONPath/etc. into the element, whose dialect this binding does not know).
        declared.Add(variable);
        return options with { DataSources = declared };
    }

    /// <summary>
    /// Builds the <see cref="DataSources"/> one element's nested evaluator resolves <c>from(...)</c> against
    /// (ADR-0010 decision 3): the element's own projected source under <paramref name="variable"/>, plus every
    /// outer-supplied source under a different name. The element binding takes priority over a like-named
    /// outer-supplied source, because inside the body <paramref name="variable"/> names the element, lexically.
    /// </summary>
    private static DataSources WithElementSource(
        DataSources? outer,
        string variable,
        TItem item,
        Func<TItem, IDataSource> projector
    )
    {
        DataSources merged = new() { { variable, new LazyElementDataSource<TItem>(item, projector) } };
        if (outer is not null)
        {
            foreach (KeyValuePair<string, IDataSource> entry in outer)
            {
                if (!merged.TryGet(entry.Key, out _))
                {
                    merged.Add(entry.Key, entry.Value);
                }
            }
        }

        return merged;
    }
}
