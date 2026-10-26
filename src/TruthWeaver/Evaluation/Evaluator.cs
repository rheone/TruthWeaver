namespace TruthWeaver.Evaluation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Metrics;
using TruthWeaver.Registry;
using Activity = System.Diagnostics.Activity;

/// <summary>
/// Walks a compiled <see cref="Expression"/> tree once, implementing the three-valued Kleene truth
/// tables (ADR-0001), left-to-right short-circuiting, per-evaluation term memoization, and fault
/// absorption (ADR-0002). A new instance is created for every call to
/// <c>CompiledRule.EvaluateAsync</c>: memoization and the fault list are scoped to exactly one
/// evaluation, never shared across calls.
/// </summary>
/// <remarks>
/// A quantifier's element iteration (ADR-0010) creates one nested <see cref="Evaluator{TContext}"/> per element,
/// over <c>Element&lt;TOuter, TItem&gt;</c> rather than this instance's own <typeparamref name="TContext"/>. Every
/// nested evaluator is given this evaluator's own <see cref="EvaluationState"/> (never a fresh one), so the fault
/// list, the flat trace log, the fault budget and <see cref="EvaluationOptions.MaxElementEvaluations"/> stay
/// whole-evaluation concepts rather than per-quantifier ones, and each is given its own element scope path, so a
/// fault or a flat trace entry it records is attributed to the element that raised it.
/// </remarks>
/// <typeparam name="TContext">The application context type.</typeparam>
internal sealed class Evaluator<TContext>(
    TContext context,
    IServiceProvider services,
    PredicateRegistry<TContext> registry,
    EvaluationOptions options,
    CancellationToken cancellationToken,
    ILogger? logger = null,
    DataSources? dataSources = null,
    EvaluationState? state = null,
    string? scope = null,
    string? memoScope = null
)
{
    private readonly TContext context = context;
    private readonly IServiceProvider services = services;
    private readonly PredicateRegistry<TContext> registry = registry;
    private readonly EvaluationOptions options = options;
    private readonly CancellationToken cancellationToken = cancellationToken;
    private readonly ILogger logger = logger ?? NullLogger.Instance;
    private readonly DataSources? dataSources = dataSources;
    private readonly EvaluationState state = state ?? new EvaluationState(options.IncludeTrace);

    // The element scope path this evaluator runs in (e.g. "line[2]" or "line[2]/tag[0]"), or null outside any
    // quantifier. Prefixes every flat trace entry this evaluator records (ADR-0010 decision 7) and labels
    // Fault.Scope; tree (TraceNode) labels are never prefixed, because the quantifier already wraps each
    // element's tree in its own "var[i]" node.
    private readonly string? scope = scope;

    // The memoization scope path this evaluator runs in, or null outside any quantifier (ADR-0010 decision 5):
    // the chain of (binding, index) pairs, for example "lines[2]/tags[0]", built from the registered collection
    // name rather than the quantifier's bound variable. This is deliberately a different string from `scope`
    // above: two quantifiers over the same binding under different variable names share this path (and so
    // share memo entries for an equal term), while the same variable name reused over different collections
    // does not, because the collection name differs.
    private readonly string? memoScope = memoScope;

    public async Task<Decision> EvaluateAsync(Expression root)
    {
        // The span ends when this method returns or throws; a cancelled or timed-out evaluation ends it untagged,
        // like the evaluations counter, which does not count it either.
        using Activity? span = TruthWeaverTracing.StartEvaluation();
        EvalResult result = await this.EvalAsync(root).ConfigureAwait(false);
        TruthWeaverMetrics.EvaluationPerformed();
        span?.SetTag(TruthWeaverTracing.DecisionTag, result.Value.ToString());
        span?.SetTag(TruthWeaverTracing.FaultCountTag, this.state.Faults.Count);
        return new Decision(
            result.Value,
            new EquatableArray<Fault>(this.state.Faults),
            this.options.IncludeTrace ? new Trace(new EquatableArray<TraceEntry>(this.state.Trace)) : null,
            result.Node
        );
    }

    /// <summary>
    /// Evaluates <paramref name="node"/> sharing this evaluator's <see cref="EvaluationState"/> and scope, without
    /// packaging a fresh <see cref="Decision"/>: the form a quantifier's collection binding uses to run a
    /// per-element body through a nested <see cref="Evaluator{TContext}"/> (over the element's own
    /// <c>Element&lt;TOuter, TItem&gt;</c> context) while every fault and flat trace entry it records lands on the
    /// same shared lists the outer evaluation's <see cref="Decision"/> is eventually built from.
    /// </summary>
    /// <param name="node">The node to evaluate (typically a quantifier's body).</param>
    /// <returns>The node's value and its trace tree.</returns>
    internal async ValueTask<(TruthValue Value, TraceNode Node)> EvaluateSharedAsync(Expression node)
    {
        EvalResult result = await this.EvalAsync(node).ConfigureAwait(false);
        return (result.Value, result.Node);
    }

    /// <summary>
    /// Renders <paramref name="identity"/> as in the canonical text but with each variable followed by the value it
    /// resolved to (<c>v: from("user", "$.age") = 18</c>). Used for the trace only, and only on request.
    /// </summary>
    private static string DescribeWithValues(TermIdentity identity, Dictionary<string, LiteralValue> values)
    {
        IEnumerable<KeyValuePair<string, string>> literals = identity.Arguments.Select(a => new KeyValuePair<string, string>(
            a.Key,
            a.Value.ToString()
        ));
        IEnumerable<KeyValuePair<string, string>> references = identity.Variables.Select(v => new KeyValuePair<string, string>(
            v.Key,
            $"{v.Value} = {values[v.Key]}"
        ));
        string arguments = string.Join(
            ", ",
            literals.Concat(references).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}: {a.Value}")
        );
        return $"{identity.PredicateName}({arguments})";
    }

    /// <summary>Gets the words the trace uses for a failed variable resolution.</summary>
    private static string OutcomeName(VariableFailureKind kind)
    {
        return kind switch
        {
            VariableFailureKind.Missing => "missing",
            VariableFailureKind.Ambiguous => "ambiguous",
            VariableFailureKind.TypeMismatch => "type mismatch",
            VariableFailureKind.SourceError => "source error",
            VariableFailureKind.UnsuppliedSource => "unsupplied source",
            _ => "failed",
        };
    }

    /// <summary>Appends a flat trace entry, prefixed with this evaluator's element scope path, if any (ADR-0010 decision 7).</summary>
    private void AddTraceEntry(string description, TruthValue? value, bool notEvaluated)
    {
        this.state.AddTrace(
            new TraceEntry(this.scope is null ? description : $"{this.scope}: {description}", value, notEvaluated)
        );
    }

    private async ValueTask<EvalResult> EvalAsync(Expression node)
    {
        if (this.state.Aborted)
        {
            string skippedDescription = TraceText.Of(node);
            this.AddTraceEntry(skippedDescription, null, true);
            return new EvalResult(
                TruthValue.Unknown,
                new TraceNode(skippedDescription, null, true, EquatableArray<TraceNode>.Empty)
            );
        }

        this.cancellationToken.ThrowIfCancellationRequested();

        switch (node)
        {
            case ConstantExpression c:
                TruthValue constantValue = c.Value;
                string constantDescription = TraceText.Of(node);
                this.AddTraceEntry(constantDescription, constantValue, false);
                return new EvalResult(
                    constantValue,
                    new TraceNode(constantDescription, constantValue, false, EquatableArray<TraceNode>.Empty)
                );
            case TermExpression t:
                return await this.EvalTermAsync(t).ConfigureAwait(false);
            case QuantifierExpression q:
                return await this.EvalQuantifierAsync(q).ConfigureAwait(false);
            default:
                // An operator node's flat entry is recorded here, after its operands' entries, because its result
                // is only known once they are evaluated (a quantifier's binding records its entry the same way).
                // The entry reuses the trace tree node's label, so both views show the same text for the node.
                EvalResult result = await this.EvalOperatorAsync(node).ConfigureAwait(false);
                this.AddTraceEntry(result.Node.Text, result.Value, false);
                return result;
        }
    }

    /// <summary>
    /// Evaluates an operator node (every node kind except a constant, a term and a quantifier). The caller records the
    /// node's flat trace entry; this method builds only its value and trace tree node.
    /// </summary>
    private async ValueTask<EvalResult> EvalOperatorAsync(Expression node)
    {
        switch (node)
        {
            case NotExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                EvalResult operand = await this.EvalAsync(shape.Operands[0]).ConfigureAwait(false);
                TruthValue value = K3Logic.Not(operand.Value);
                return new EvalResult(
                    value,
                    new TraceNode(TraceText.LabelOf(shape), value, false, new EquatableArray<TraceNode>([operand.Node]))
                );
            }

            case AndExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync(
                        TraceText.LabelOf(shape),
                        shape.Operands,
                        TruthValue.True,
                        K3Logic.And,
                        stops: value => value == TruthValue.False
                    )
                    .ConfigureAwait(false);
            }

            case OrExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync(
                        TraceText.LabelOf(shape),
                        shape.Operands,
                        TruthValue.False,
                        K3Logic.Or,
                        stops: value => value == TruthValue.True
                    )
                    .ConfigureAwait(false);
            }

            case CoalesceExpression:
            {
                // The first non-Unknown operand wins and later operands cannot change it, so (outside
                // exhaustive mode) they are skipped and marked as such, exactly like AND/OR short-circuit.
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync(
                        TraceText.LabelOf(shape),
                        shape.Operands,
                        TruthValue.Unknown,
                        (accumulated, next) => accumulated == TruthValue.Unknown ? next : accumulated,
                        stops: value => value != TruthValue.Unknown
                    )
                    .ConfigureAwait(false);
            }

            case InspectionExpression inspection:
            {
                NodeShape shape = ExpressionShape.Of(node);
                EvalResult operand = await this.EvalAsync(shape.Operands[0]).ConfigureAwait(false);
                TruthValue value = K3Logic.Inspect(inspection.Kind, operand.Value);
                return new EvalResult(
                    value,
                    new TraceNode(TraceText.LabelOf(shape), value, false, new EquatableArray<TraceNode>([operand.Node]))
                );
            }

            case IfExpression ifNode:
                return await this.EvalIfAsync(ifNode).ConfigureAwait(false);

            case XorExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Xor(results[0].Value, results[1].Value);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case EquivalentExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Equivalent(results[0].Value, results[1].Value);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case ImpliesExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Implies(results[0].Value, results[1].Value);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case NandExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Nand(results[0].Value, results[1].Value);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case NorExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Nor(results[0].Value, results[1].Value);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case ParityExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Parity([.. results.Select(r => r.Value)]);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case AnyExpression:
            {
                // ANY is AtLeast(1, ...) over the definitely-true / possibly-true interval (ADR-0005 decision 6).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Threshold(ThresholdComparison.AtLeast, 1, [.. results.Select(r => r.Value)]);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case AllExpression:
            {
                // ALL is AtLeast(n, ...) for the n operands.
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Threshold(
                    ThresholdComparison.AtLeast,
                    results.Count,
                    [.. results.Select(r => r.Value)]
                );
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case NoneExpression:
            {
                // NONE is AtMost(0, ...).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Threshold(ThresholdComparison.AtMost, 0, [.. results.Select(r => r.Value)]);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case ExactlyOneExpression:
            {
                // ExactlyOne is Exactly(1, ...) over the same definite/possible interval as every other
                // cardinality operator (it folds into the one threshold path, with no separate evaluation).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Threshold(ThresholdComparison.Exactly, 1, [.. results.Select(r => r.Value)]);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case BetweenExpression bt:
            {
                // AND(AtLeast(min, ...), AtMost(max, ...)) over the same operand values (ADR-0005 decision 6).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue[] operandValues = [.. results.Select(r => r.Value)];
                TruthValue value = K3Logic.And(
                    K3Logic.Threshold(ThresholdComparison.AtLeast, bt.Min, operandValues),
                    K3Logic.Threshold(ThresholdComparison.AtMost, bt.Max, operandValues)
                );
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            case ThresholdExpression th:
            {
                // Every operand is evaluated: the test needs both the definite and the possible true count, so a
                // threshold never short-circuits. The cost is linear in the operand count.
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = K3Logic.Threshold(th.Comparison, th.K, [.. results.Select(r => r.Value)]);
                return new EvalResult(
                    value,
                    new TraceNode(
                        TraceText.LabelOf(shape),
                        value,
                        false,
                        new EquatableArray<TraceNode>(results.Select(r => r.Node))
                    )
                );
            }

            default:
                throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'.");
        }
    }

    /// <summary>
    /// Evaluates <c>If(condition, whenTrue, whenFalse)</c>. A definite condition needs only its own branch, so the other
    /// is skipped (marked <c>NotEvaluated</c>, like the operands AND/OR short-circuit past). An <c>Unknown</c> condition
    /// cannot choose, so both branches are evaluated and the result is their shared definite value or <c>Unknown</c>.
    /// <see cref="EvaluationMode.Exhaustive"/> evaluates both branches regardless.
    /// </summary>
    private async ValueTask<EvalResult> EvalIfAsync(IfExpression node)
    {
        EvalResult condition = await this.EvalAsync(node.Condition).ConfigureAwait(false);
        bool exhaustive = this.options.Mode == EvaluationMode.Exhaustive;
        bool needWhenTrue = exhaustive || condition.Value != TruthValue.False;
        bool needWhenFalse = exhaustive || condition.Value != TruthValue.True;

        EvalResult whenTrue = needWhenTrue
            ? await this.EvalAsync(node.WhenTrue).ConfigureAwait(false)
            : this.Skip(node.WhenTrue);
        EvalResult whenFalse = needWhenFalse
            ? await this.EvalAsync(node.WhenFalse).ConfigureAwait(false)
            : this.Skip(node.WhenFalse);

        TruthValue value = condition.Value switch
        {
            TruthValue.True => whenTrue.Value,
            TruthValue.False => whenFalse.Value,

            // No branch can be chosen: the answer is only certain when both branches agree on a definite value.
            _ => whenTrue.Value == whenFalse.Value ? whenTrue.Value : TruthValue.Unknown,
        };
        string label = TraceText.LabelOf(ExpressionShape.Of(node));
        return new EvalResult(
            value,
            new TraceNode(label, value, false, new EquatableArray<TraceNode>([condition.Node, whenTrue.Node, whenFalse.Node]))
        );
    }

    /// <summary>Records <paramref name="node"/> as not evaluated in the trace and trace tree.</summary>
    private EvalResult Skip(Expression node)
    {
        string skippedDescription = TraceText.Of(node);
        this.AddTraceEntry(skippedDescription, null, true);
        return new EvalResult(
            TruthValue.Unknown,
            new TraceNode(skippedDescription, null, true, EquatableArray<TraceNode>.Empty)
        );
    }

    /// <summary>Evaluates a quantifier node by delegating to its collection binding (ADR-0010).</summary>
    private async ValueTask<EvalResult> EvalQuantifierAsync(QuantifierExpression node)
    {
        string label = QuantifierText.Label(node.Kind);
        string description = $"{label}({node.Variable} IN {node.CollectionName})";
        if (!this.registry.TryGetCollection(node.CollectionName, out ICollectionBinding<TContext>? binding))
        {
            // Defensive only: a successfully Strict-mode-compiled rule cannot reference an unregistered collection.
            // If it ever runs, the fault is recorded against the collection name, as a throwing selector's is.
            this.RecordFault(
                new TermIdentity(node.CollectionName, []),
                new KeyNotFoundException($"No collection named '{node.CollectionName}' is registered.")
            );
            this.AddTraceEntry(description, TruthValue.Unknown, false);
            return new EvalResult(
                TruthValue.Unknown,
                new TraceNode(description, TruthValue.Unknown, false, EquatableArray<TraceNode>.Empty)
            );
        }

        (TruthValue value, TraceNode traceNode) = await binding
            .EvaluateQuantifierAsync(
                this.context,
                node.Kind,
                node.Variable,
                node.Body,
                this.options,
                this.state,
                this.services,
                this.logger,
                this.dataSources,
                this.scope,
                this.memoScope,
                this.cancellationToken
            )
            .ConfigureAwait(false);
        return new EvalResult(value, traceNode);
    }

    /// <summary>
    /// Folds the operands of an <c>AND</c>, <c>OR</c> or <c>COALESCE</c> chain from left to right, starting from
    /// <paramref name="identity"/> (<c>True</c> for <c>AND</c>, <c>False</c> for <c>OR</c>, <c>Unknown</c> for
    /// <c>COALESCE</c>). Once an operand produces a value for which <paramref name="stops"/> holds, the rest are skipped
    /// and recorded as <c>NotEvaluated</c>, unless <see cref="EvaluationMode.Exhaustive"/> is on. The stop value absorbs
    /// every later operand, so a short-circuit stop never changes the result. An operand skipped because the fault
    /// budget aborted the evaluation is also recorded as <c>NotEvaluated</c>, and it folds into the result as
    /// <c>Unknown</c>, because its real value could still change the result.
    /// </summary>
    private async ValueTask<EvalResult> EvalChainAsync(
        string description,
        IReadOnlyList<Expression> operands,
        TruthValue identity,
        Func<TruthValue, TruthValue, TruthValue> combine,
        Func<TruthValue, bool> stops
    )
    {
        TruthValue accumulator = identity;
        List<TraceNode> children = [with(operands.Count)];
        bool exhaustive = this.options.Mode == EvaluationMode.Exhaustive;
        bool stop = false;
        foreach (Expression operand in operands)
        {
            if (stop || this.state.Aborted)
            {
                string skippedDescription = TraceText.Of(operand);
                this.AddTraceEntry(skippedDescription, null, true);
                children.Add(new TraceNode(skippedDescription, null, true, EquatableArray<TraceNode>.Empty));

                // An operand skipped by a fault-budget abort could still decide the chain, so it counts as Unknown,
                // as a skipped single node does in EvalAsync. A short-circuit skip needs no fold: the accumulator
                // already holds the stop value, which Unknown cannot change.
                if (!stop)
                {
                    accumulator = combine(accumulator, TruthValue.Unknown);
                }

                continue;
            }

            EvalResult result = await this.EvalAsync(operand).ConfigureAwait(false);
            children.Add(result.Node);
            accumulator = combine(accumulator, result.Value);
            if (!exhaustive && stops(result.Value))
            {
                stop = true;
            }
        }

        return new EvalResult(
            accumulator,
            new TraceNode(description, accumulator, false, new EquatableArray<TraceNode>(children))
        );
    }

    private async ValueTask<IReadOnlyList<EvalResult>> EvalAllAsync(IReadOnlyList<Expression> operands)
    {
        List<EvalResult> values = [with(operands.Count)];
        foreach (Expression operand in operands)
        {
            values.Add(await this.EvalAsync(operand).ConfigureAwait(false));
        }

        return values;
    }

    private async ValueTask<EvalResult> EvalTermAsync(TermExpression term)
    {
        string description = term.Identity.ToString();
        (string? Scope, TermIdentity Identity) key = (this.memoScope, term.Identity);
        if (term.IsUnknownPredicate)
        {
            // An unregistered predicate compiled under CompilationMode.Lenient is a permanent Unknown fault on its
            // term (ADR-0002): the predicate is never invoked, but the fault says why the result is Unknown. The
            // memo makes a repeat of the term in the same scope report one fault, like any other term.
            if (this.state.Memo.TryAdd(key, TruthValue.Unknown))
            {
                this.RecordFault(
                    term.Identity,
                    new KeyNotFoundException(
                        $"No predicate named '{term.Identity.PredicateName}' is registered (CompilationMode.Lenient)."
                    )
                );
            }

            this.AddTraceEntry(description, TruthValue.Unknown, false);
            return new EvalResult(
                TruthValue.Unknown,
                new TraceNode(description, TruthValue.Unknown, false, EquatableArray<TraceNode>.Empty)
            );
        }

        if (this.state.Memo.TryGetValue(key, out TruthValue cached))
        {
            // A repeat of a term that resolved earlier shows the same values as its first occurrence.
            description = this.Annotated(key, description);
            this.AddTraceEntry(description, cached, false);
            return new EvalResult(cached, new TraceNode(description, cached, false, EquatableArray<TraceNode>.Empty));
        }

        TruthValue result = await this.InvokeAsync(term.Identity).ConfigureAwait(false);
        this.state.Memo[key] = result;
        description = this.Annotated(key, description);
        this.AddTraceEntry(description, result, false);
        return new EvalResult(result, new TraceNode(description, result, false, EquatableArray<TraceNode>.Empty));
    }

    /// <summary>Returns the trace text of a term: its resolved-value form when one was recorded, followed by the outcome of each variable reference and a note for each array reference that matched no node.</summary>
    private string Annotated((string? Scope, TermIdentity Identity) key, string description)
    {
        string text = this.state.ValueDescriptions.GetValueOrDefault(key, description);

        // Every variable reference of the term names its outcome, so a resolved reference is visible too (ADR-0006 decision 13).
        if (this.state.VariableOutcomes.TryGetValue(key, out List<string>? outcomes))
        {
            text += string.Concat(outcomes.Select(o => $" [{o}]"));
        }

        if (this.state.ZeroMatchReferences.TryGetValue(key, out List<VariableReference>? references))
        {
            text += string.Concat(references.Select(r => $" [no match for {r}]"));
        }

        return text;
    }

    private async ValueTask<TruthValue> InvokeAsync(TermIdentity identity)
    {
        if (!this.options.TracePredicates)
        {
            return await this.InvokeCoreAsync(identity).ConfigureAwait(false);
        }

        // Opt-in child span of the evaluation span (or of the enclosing span, inside a quantifier). A memoized
        // repeat of a term never reaches here, so the span count is the number of distinct invocations.
        using Activity? span = TruthWeaverTracing.StartPredicate(identity.PredicateName);
        int faultsBefore = this.state.Faults.Count;
        TruthValue result = await this.InvokeCoreAsync(identity).ConfigureAwait(false);
        span?.SetTag(TruthWeaverTracing.PredicateResultTag, result.ToString());
        span?.SetTag(TruthWeaverTracing.PredicateFaultedTag, this.state.Faults.Count > faultsBefore);
        return result;
    }

    private async ValueTask<TruthValue> InvokeCoreAsync(TermIdentity identity)
    {
        if (!this.registry.TryGet(identity.PredicateName, out PredicateDescriptor<TContext>? descriptor))
        {
            // Defensive only: a successfully Strict-mode-compiled rule cannot reference an unregistered predicate.
            // If it ever runs, the fault says why the term is Unknown (ADR-0002).
            this.RecordFault(
                identity,
                new KeyNotFoundException($"No predicate named '{identity.PredicateName}' is registered.")
            );
            return TruthValue.Unknown;
        }

        try
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            Dictionary<string, LiteralValue> values = identity.Arguments.ToDictionary(kv => kv.Key, kv => kv.Value);
            if (
                identity.Variables.Count > 0
                && !await this.ResolveVariablesAsync(identity, descriptor, values).ConfigureAwait(false)
            )
            {
                // The predicate is not called with a missing argument: the term is Unknown and each failure is a fault.
                return TruthValue.Unknown;
            }

            if (this.options.IncludeResolvedValues && identity.Variables.Count > 0)
            {
                this.state.ValueDescriptions[(this.memoScope, identity)] = DescribeWithValues(identity, values);
            }

            PredicateArguments args = new(values);

            // A predicate may answer Unknown directly; that is a normal value, not a fault. Only a throw
            // (including a timeout or cancellation surfaced as an exception) is recorded as a Fault.
            return descriptor.Evaluate is { } lambda
                ? await lambda(this.context, args, this.cancellationToken).ConfigureAwait(false)
                : await this.InvokeClassBasedAsync(descriptor, args).ConfigureAwait(false);
        }
        catch (Exception ex)
            when (ex is not TestDoubleMisuseException
                && (ex is not OperationCanceledException || !this.cancellationToken.IsCancellationRequested)
            )
        {
            // A misused test double (TruthWeaver.Testing) is not a predicate failure: it escapes so the test fails.
            this.RecordFault(identity, ex);
            return TruthValue.Unknown;
        }
    }

    /// <summary>Records a fault for <paramref name="identity"/> and aborts the evaluation once the fault count reaches the fault budget.</summary>
    private void RecordFault(TermIdentity identity, Exception ex)
    {
        FaultRecorder.Record(this.state, this.logger, this.options, this.scope, identity, ex);
    }

    /// <summary>
    /// Resolves every variable argument of a term into <paramref name="values"/> (ADR-0006 decisions 6 and 7). Every
    /// variable is attempted, so one term with two bad references records two faults.
    /// </summary>
    /// <returns><see langword="true"/> if all variables resolved; otherwise a fault was recorded for each failure.</returns>
    private async ValueTask<bool> ResolveVariablesAsync(
        TermIdentity identity,
        PredicateDescriptor<TContext> descriptor,
        Dictionary<string, LiteralValue> values
    )
    {
        bool resolved = true;
        List<string> outcomes = [];
        foreach ((string argumentName, VariableReference reference) in identity.Variables)
        {
            VariableResolutionException? failure = null;
            SourceAnswer answer = await this.QuerySourceAsync(reference).ConfigureAwait(false);
            PredicateArgumentSchema? argument = descriptor.Schema.Arguments.FirstOrDefault(a =>
                string.Equals(a.Name, argumentName, StringComparison.Ordinal)
            );
            if (answer.Failure is { } sourceFailure)
            {
                failure = sourceFailure;
            }
            else if (argument is null)
            {
                // Defensive only: the compiler matched the argument name against this schema.
                failure = new VariableResolutionException(
                    reference,
                    VariableFailureKind.TypeMismatch,
                    $"Predicate '{identity.PredicateName}' declares no argument '{argumentName}'."
                );
            }
            else if (
                VariableConversion.TryConvert(
                    answer.Matches,
                    argument.Type,
                    out LiteralValue value,
                    out VariableFailureKind kind,
                    out string message
                )
            )
            {
                values[argumentName] = value;
                if (answer.Matches.Count == 0 && VariableConversion.IsArrayKind(argument.Type))
                {
                    // An empty array is a valid argument, so only the trace says the query found nothing.
                    (string? Scope, TermIdentity Identity) key = (this.memoScope, identity);
                    if (!this.state.ZeroMatchReferences.TryGetValue(key, out List<VariableReference>? noted))
                    {
                        noted = [];
                        this.state.ZeroMatchReferences[key] = noted;
                    }

                    noted.Add(reference);
                }
            }
            else
            {
                failure = new VariableResolutionException(reference, kind, $"{reference}: {message}");
            }

            // The trace names the reference and what happened to it, never the value (ADR-0006 decision 13).
            outcomes.Add($"{reference}: {(failure is null ? "resolved" : OutcomeName(failure.Kind))}");
            if (failure is not null)
            {
                resolved = false;
                this.RecordFault(identity, failure);
            }
        }

        // Keyed by the memoization scope, as Annotated reads it, so a term inside a quantifier body keeps its notes.
        this.state.VariableOutcomes[(this.memoScope, identity)] = outcomes;
        return resolved;
    }

    /// <summary>Asks the named source a query, once per evaluation; a failure is stored and replayed like a value.</summary>
    private async ValueTask<SourceAnswer> QuerySourceAsync(VariableReference reference)
    {
        (string? Scope, VariableReference Reference) key = (this.memoScope, reference);
        if (this.state.SourceAnswers.TryGetValue(key, out SourceAnswer? known))
        {
            return known;
        }

        SourceAnswer answer = await this.QuerySourceUncachedAsync(reference).ConfigureAwait(false);
        this.state.SourceAnswers[key] = answer;
        return answer;
    }

    private async ValueTask<SourceAnswer> QuerySourceUncachedAsync(VariableReference reference)
    {
        if (this.dataSources is null || !this.dataSources.TryGet(reference.Source, out IDataSource? source))
        {
            return SourceAnswer.Failed(
                new VariableResolutionException(
                    reference,
                    VariableFailureKind.UnsuppliedSource,
                    $"{reference}: data source '{reference.Source}' was not supplied to this evaluation."
                )
            );
        }

        DataQueryResult? result;
        try
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            result = await source.QueryAsync(reference.Query, this.cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !this.cancellationToken.IsCancellationRequested)
        {
            // A source's own timeout or cancellation (the evaluation's token is not cancelled) is a source error;
            // cancellation of the evaluation itself propagates exactly as it does from a predicate.
            return SourceAnswer.Failed(
                new VariableResolutionException(
                    reference,
                    VariableFailureKind.SourceError,
                    $"{reference}: data source '{reference.Source}' failed ({ex.GetType().Name}).",
                    ex
                )
            );
        }

        if (result is null)
        {
            return SourceAnswer.Failed(
                new VariableResolutionException(
                    reference,
                    VariableFailureKind.SourceError,
                    $"{reference}: data source '{reference.Source}' returned no result."
                )
            );
        }

        if (result.ErrorKind is { } errorKind)
        {
            // A node with no literal equivalent (an object) can never fit the argument, so it is a type mismatch;
            // a malformed query or failing backend is a source error.
            VariableFailureKind kind =
                errorKind == DataQueryErrorKind.UnsupportedType
                    ? VariableFailureKind.TypeMismatch
                    : VariableFailureKind.SourceError;
            return SourceAnswer.Failed(
                new VariableResolutionException(reference, kind, $"{reference}: {errorKind}: {result.ErrorMessage}")
            );
        }

        return SourceAnswer.Answered(result.Matches);
    }

    private ValueTask<TruthValue> InvokeClassBasedAsync(PredicateDescriptor<TContext> descriptor, PredicateArguments args)
    {
        object? instance =
            this.services.GetService(descriptor.ImplementationType!)
            ?? throw new InvalidOperationException(
                $"No service is registered for predicate implementation type '{descriptor.ImplementationType}'. Register it with the application's IServiceProvider."
            );
        IPredicate<TContext> predicate = (IPredicate<TContext>)instance;
        return predicate.EvaluateAsync(this.context, args, this.cancellationToken);
    }

    /// <summary>
    /// One node's outcome, paired with a structural <see cref="TraceNode"/> mirroring the shape
    /// <see cref="Expression"/> is recursed over. Every recursive evaluation step returns one of these
    /// instead of a bare <see cref="TruthValue"/>, so the per-node annotations needed for
    /// <see cref="Decision.TraceTree"/> fall out of the existing recursion for free, with no
    /// separate replay pass.
    /// </summary>
    private readonly record struct EvalResult(TruthValue Value, TraceNode Node);
}
