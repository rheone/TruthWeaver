namespace TruthWeaver.Registry;

using Microsoft.Extensions.Logging;
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
internal sealed class CollectionBinding<TContext, TItem>(
    string name,
    Func<TContext, IEnumerable<TItem>?> selector,
    PredicateRegistry<Element<TContext, TItem>> elementRegistry
) : ICollectionBinding<TContext>
{
    public string Name => name;

    public (Expression? Tree, IReadOnlyList<Diagnostic> Diagnostics) CompileBody(
        RuleNode body,
        CompilerOptions options,
        int depth,
        int quantifierDepth,
        IReadOnlyList<string> enclosingVariables
    )
    {
        (Expression? tree, RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = RuleNodeCompiler<
            Element<TContext, TItem>
        >.Compile(body, elementRegistry, options, depth, quantifierDepth, enclosingVariables);
        return (tree, diagnostics);
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
        CancellationToken cancellationToken
    )
    {
        string label = kind == QuantifierKind.ForAll ? "FORALL" : "EXISTS";
        string description = $"{label}({variable} IN {name})";

        void AddFlat(TruthValue? value, bool notEvaluated)
        {
            state.Trace.Add(
                new TraceEntry(outerScope is null ? description : $"{outerScope}: {description}", value, notEvaluated)
            );
        }

        IReadOnlyList<TItem>? elements;
        try
        {
            elements = selector(context)?.ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // ADR-0010 decision 4: a throwing selector is Unknown plus one fault for the collection itself.
            state.Faults.Add(new Fault(new TermIdentity(name, []), ex, outerScope));
            if (options.FaultBudget is { } budget && state.Faults.Count >= budget)
            {
                state.Aborted = true;
            }

            AddFlat(TruthValue.Unknown, false);
            return (TruthValue.Unknown, new TraceNode(description, TruthValue.Unknown, false, EquatableArray<TraceNode>.Empty));
        }

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
        // what the elements reached so far folded to — unlike a fault-budget abort (which, "exactly as today",
        // keeps whatever the fold reached before the remaining elements were skipped).
        bool elementLimitExceeded = false;
        List<TraceNode> children = [];
        for (int i = 0; i < elements.Count; i++)
        {
            string elementLabel = $"{variable}[{i}]";
            string elementScope = outerScope is null ? elementLabel : $"{outerScope}/{elementLabel}";

            if (stop || state.Aborted)
            {
                TraceNode skippedBody = new(TraceText.Of(body), null, true, EquatableArray<TraceNode>.Empty);
                children.Add(new TraceNode(elementLabel, null, true, new EquatableArray<TraceNode>([skippedBody])));
                continue;
            }

            if (state.ElementEvaluations >= options.MaxElementEvaluations)
            {
                // ADR-0010 decision 6: over the bound, the current quantifier is Unknown with one fault and the
                // whole evaluation aborts, the same way the fault budget aborts.
                state.Faults.Add(
                    new Fault(
                        new TermIdentity(name, []),
                        new ElementEvaluationLimitExceededException(options.MaxElementEvaluations),
                        outerScope
                    )
                );
                state.Aborted = true;
                elementLimitExceeded = true;
                TraceNode skippedBody = new(TraceText.Of(body), null, true, EquatableArray<TraceNode>.Empty);
                children.Add(new TraceNode(elementLabel, null, true, new EquatableArray<TraceNode>([skippedBody])));
                continue;
            }

            state.ElementEvaluations++;
            Element<TContext, TItem> elementContext = new(context, elements[i], i);
            Evaluator<Element<TContext, TItem>> nested = new(
                elementContext,
                services,
                elementRegistry,
                options,
                cancellationToken,
                logger,
                dataSources,
                state,
                elementScope
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
}
