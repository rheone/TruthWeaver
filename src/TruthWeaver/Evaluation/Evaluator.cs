namespace TruthWeaver.Evaluation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Logging;
using TruthWeaver.Metrics;
using TruthWeaver.Registry;

/// <summary>
/// Walks a compiled <see cref="Expression"/> tree once, implementing the three-valued Kleene truth
/// tables (ADR-0001), left-to-right short-circuiting, per-evaluation term memoization, and fault
/// absorption (ADR-0002). A new instance is created for every call to
/// <c>CompiledRule.EvaluateAsync</c> — memoization and the fault list are scoped to exactly one
/// evaluation, never shared across calls.
/// </summary>
/// <typeparam name="TContext">The application context type.</typeparam>
internal sealed class Evaluator<TContext>(
    TContext context,
    IServiceProvider services,
    PredicateRegistry<TContext> registry,
    EvaluationOptions options,
    CancellationToken cancellationToken,
    ILogger? logger = null,
    DataSources? dataSources = null
)
{
    private readonly TContext context = context;
    private readonly IServiceProvider services = services;
    private readonly PredicateRegistry<TContext> registry = registry;
    private readonly EvaluationOptions options = options;
    private readonly CancellationToken cancellationToken = cancellationToken;
    private readonly ILogger logger = logger ?? NullLogger.Instance;
    private readonly DataSources? dataSources = dataSources;
    private readonly Dictionary<TermIdentity, TruthValue> memo = [];

    // Trace text of terms whose variables resolved, with the values shown; filled only when IncludeResolvedValues is set.
    private readonly Dictionary<TermIdentity, string> valueDescriptions = [];

    // What each (source, query) pair answered, queried at most once per evaluation (ADR-0006 decision 11). A failure is
    // stored like a value, so a repeated reference neither re-queries the source nor re-runs a failing call.
    private readonly Dictionary<VariableReference, SourceAnswer> sourceAnswers = [];
    private readonly List<Fault> faults = [];
    private readonly List<TraceEntry> trace = [];
    private bool aborted;

    public async Task<Decision> EvaluateAsync(Expression root)
    {
        EvalResult result = await this.EvalAsync(root).ConfigureAwait(false);
        TruthWeaverMetrics.EvaluationPerformed();
        return new Decision(result.Value, this.faults, new Trace(this.trace), result.Node);
    }

    private static string NodeText(Expression node)
    {
        if (node is ConstantExpression c)
        {
            return TruthValueText.Canonical(c.Value);
        }

        if (node is TermExpression t)
        {
            return t.Identity.ToString();
        }

        NodeShape shape = ExpressionShape.Of(node);

        // Labels come from the operator table; the completeness test guarantees every operator has one, so the
        // op-name fallback is only reachable for a node that skipped the table.
        return OperatorDefinitions.TryGet(shape.OpName, out OperatorDefinition? definition)
            ? definition.Label(shape)
            : shape.OpName;
    }

    /// <summary>Tests the K3 state of <paramref name="value"/>; the answer is always a definite <c>True</c> or <c>False</c>.</summary>
    private static TruthValue Inspect(InspectionKind kind, TruthValue value)
    {
        bool matches = kind switch
        {
            InspectionKind.IsTrue => value == TruthValue.True,
            InspectionKind.IsFalse => value == TruthValue.False,
            InspectionKind.IsUnknown => value == TruthValue.Unknown,
            InspectionKind.IsKnown => value != TruthValue.Unknown,
            _ => throw new InvalidOperationException($"Unhandled inspection kind '{kind}'."),
        };
        return matches ? TruthValue.True : TruthValue.False;
    }

    private static TruthValue KleeneAnd(TruthValue a, TruthValue b)
    {
        return (a, b) switch
        {
            (TruthValue.False, _) => TruthValue.False,
            (_, TruthValue.False) => TruthValue.False,
            (TruthValue.True, TruthValue.True) => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    private static TruthValue KleeneOr(TruthValue a, TruthValue b)
    {
        return (a, b) switch
        {
            (TruthValue.True, _) => TruthValue.True,
            (_, TruthValue.True) => TruthValue.True,
            (TruthValue.False, TruthValue.False) => TruthValue.False,
            _ => TruthValue.Unknown,
        };
    }

    private static TruthValue KleeneNot(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    private static TruthValue KleeneXor(TruthValue left, TruthValue right)
    {
        if (left == TruthValue.Unknown || right == TruthValue.Unknown)
        {
            return TruthValue.Unknown;
        }

        return (left == TruthValue.True) ^ (right == TruthValue.True) ? TruthValue.True : TruthValue.False;
    }

    private static TruthValue KleeneEquivalent(TruthValue left, TruthValue right)
    {
        return KleeneNot(KleeneXor(left, right));
    }

    /// <summary>Strong Kleene negated conjunction: <c>NOT (left AND right)</c>.</summary>
    private static TruthValue KleeneNand(TruthValue left, TruthValue right)
    {
        return KleeneNot(KleeneAnd(left, right));
    }

    /// <summary>Strong Kleene negated disjunction: <c>NOT (left OR right)</c>.</summary>
    private static TruthValue KleeneNor(TruthValue left, TruthValue right)
    {
        return KleeneNot(KleeneOr(left, right));
    }

    /// <summary>Strong Kleene material implication: <c>NOT antecedent OR consequent</c>.</summary>
    private static TruthValue KleeneImplies(TruthValue antecedent, TruthValue consequent)
    {
        return KleeneOr(KleeneNot(antecedent), consequent);
    }

    /// <summary>
    /// Strong Kleene n-ary parity: <c>Unknown</c> if any operand is <c>Unknown</c> (the fold of binary XOR, which is
    /// <c>Unknown</c> whenever either side is), otherwise <c>True</c> for an odd number of <c>True</c> operands.
    /// </summary>
    private static TruthValue EvaluateParity(IReadOnlyList<TruthValue> operandValues)
    {
        if (operandValues.Any(v => v == TruthValue.Unknown))
        {
            return TruthValue.Unknown;
        }

        return operandValues.Count(v => v == TruthValue.True) % 2 == 1 ? TruthValue.True : TruthValue.False;
    }

    private static TruthValue EvaluateExactlyOne(IReadOnlyList<TruthValue> operandValues)
    {
        int trueCount = operandValues.Count(v => v == TruthValue.True);
        int unknownCount = operandValues.Count(v => v == TruthValue.Unknown);

        if (trueCount >= 2)
        {
            return TruthValue.False;
        }

        if (unknownCount > 0)
        {
            return TruthValue.Unknown;
        }

        return trueCount == 1 ? TruthValue.True : TruthValue.False;
    }

    /// <summary>
    /// Evaluates any count-threshold comparison against the range of true-operand counts still
    /// reachable given how many operands remain <see cref="TruthValue.Unknown"/> — determinate only
    /// when the comparison agrees at both the lowest and highest possible count (monotonic
    /// comparisons) or when there is no remaining ambiguity at all (<see cref="ThresholdComparison.Exactly"/>,
    /// which is not monotonic in the count).
    /// </summary>
    private static TruthValue EvaluateThreshold(ThresholdComparison comparison, int k, IReadOnlyList<TruthValue> operandValues)
    {
        int trueCount = operandValues.Count(v => v == TruthValue.True);
        int unknownCount = operandValues.Count(v => v == TruthValue.Unknown);
        int minCount = trueCount;
        int maxCount = trueCount + unknownCount;

        if (comparison == ThresholdComparison.Exactly)
        {
            if (k < minCount || k > maxCount)
            {
                return TruthValue.False;
            }

            return unknownCount == 0 ? TruthValue.True : TruthValue.Unknown;
        }

        bool SatisfiesAt(int count) =>
            comparison switch
            {
                ThresholdComparison.AtLeast => count >= k,
                ThresholdComparison.AtMost => count <= k,
                ThresholdComparison.GreaterThan => count > k,
                ThresholdComparison.LessThan => count < k,
                _ => throw new InvalidOperationException($"Unhandled threshold comparison '{comparison}'."),
            };

        bool satisfiesMin = SatisfiesAt(minCount);
        bool satisfiesMax = SatisfiesAt(maxCount);
        if (satisfiesMin && satisfiesMax)
        {
            return TruthValue.True;
        }

        return !satisfiesMin && !satisfiesMax ? TruthValue.False : TruthValue.Unknown;
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

    private async ValueTask<EvalResult> EvalAsync(Expression node)
    {
        if (this.aborted)
        {
            string skippedDescription = NodeText(node);
            this.trace.Add(new TraceEntry(skippedDescription, null, true));
            return new EvalResult(TruthValue.Unknown, new TraceNode(skippedDescription, null, true, []));
        }

        this.cancellationToken.ThrowIfCancellationRequested();

        switch (node)
        {
            case ConstantExpression c:
                TruthValue constantValue = c.Value;
                string constantDescription = NodeText(node);
                this.trace.Add(new TraceEntry(constantDescription, constantValue, false));
                return new EvalResult(constantValue, new TraceNode(constantDescription, constantValue, false, []));
            case TermExpression t:
                return await this.EvalTermAsync(t).ConfigureAwait(false);
            case NotExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                EvalResult operand = await this.EvalAsync(shape.Operands[0]).ConfigureAwait(false);
                TruthValue value = KleeneNot(operand.Value);
                return new EvalResult(value, new TraceNode("NOT", value, false, [operand.Node]));
            }

            case AndExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync(
                        "AND",
                        shape.Operands,
                        TruthValue.True,
                        KleeneAnd,
                        stops: value => value == TruthValue.False
                    )
                    .ConfigureAwait(false);
            }

            case OrExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync(
                        "OR",
                        shape.Operands,
                        TruthValue.False,
                        KleeneOr,
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
                        "COALESCE",
                        shape.Operands,
                        TruthValue.Unknown,
                        (accumulated, next) => accumulated == TruthValue.Unknown ? next : accumulated,
                        stops: value => value != TruthValue.Unknown
                    )
                    .ConfigureAwait(false);
            }

            case InspectionExpression inspection:
            {
                EvalResult operand = await this.EvalAsync(inspection.Operand).ConfigureAwait(false);
                TruthValue value = Inspect(inspection.Kind, operand.Value);
                return new EvalResult(value, new TraceNode(inspection.Kind.ToString(), value, false, [operand.Node]));
            }

            case IfExpression ifNode:
                return await this.EvalIfAsync(ifNode).ConfigureAwait(false);

            case XorExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneXor(results[0].Value, results[1].Value);
                return new EvalResult(value, new TraceNode("XOR", value, false, [.. results.Select(r => r.Node)]));
            }

            case EquivalentExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneEquivalent(results[0].Value, results[1].Value);
                return new EvalResult(value, new TraceNode("EQUIVALENT", value, false, [.. results.Select(r => r.Node)]));
            }

            case ImpliesExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneImplies(results[0].Value, results[1].Value);
                return new EvalResult(value, new TraceNode("IMPLIES", value, false, [.. results.Select(r => r.Node)]));
            }

            case NandExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneNand(results[0].Value, results[1].Value);
                return new EvalResult(value, new TraceNode("NAND", value, false, [.. results.Select(r => r.Node)]));
            }

            case NorExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneNor(results[0].Value, results[1].Value);
                return new EvalResult(value, new TraceNode("NOR", value, false, [.. results.Select(r => r.Node)]));
            }

            case ParityExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateParity([.. results.Select(r => r.Value)]);
                return new EvalResult(value, new TraceNode("PARITY", value, false, [.. results.Select(r => r.Node)]));
            }

            case AnyExpression:
            {
                // ANY is AtLeast(1, ...) over the definitely-true / possibly-true interval (ADR-0005 decision 6).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateThreshold(ThresholdComparison.AtLeast, 1, [.. results.Select(r => r.Value)]);
                return new EvalResult(value, new TraceNode("ANY", value, false, [.. results.Select(r => r.Node)]));
            }

            case AllExpression:
            {
                // ALL is AtLeast(n, ...) for the n operands.
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateThreshold(
                    ThresholdComparison.AtLeast,
                    results.Count,
                    [.. results.Select(r => r.Value)]
                );
                return new EvalResult(value, new TraceNode("ALL", value, false, [.. results.Select(r => r.Node)]));
            }

            case NoneExpression:
            {
                // NONE is AtMost(0, ...).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateThreshold(ThresholdComparison.AtMost, 0, [.. results.Select(r => r.Value)]);
                return new EvalResult(value, new TraceNode("NONE", value, false, [.. results.Select(r => r.Node)]));
            }

            case ExactlyOneExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateExactlyOne([.. results.Select(r => r.Value)]);
                return new EvalResult(value, new TraceNode("ExactlyOne", value, false, [.. results.Select(r => r.Node)]));
            }

            case BetweenExpression bt:
            {
                // AND(AtLeast(min, ...), AtMost(max, ...)) over the same operand values (ADR-0005 decision 6).
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue[] operandValues = [.. results.Select(r => r.Value)];
                TruthValue value = KleeneAnd(
                    EvaluateThreshold(ThresholdComparison.AtLeast, bt.Min, operandValues),
                    EvaluateThreshold(ThresholdComparison.AtMost, bt.Max, operandValues)
                );
                string description = NodeText(node);
                return new EvalResult(value, new TraceNode(description, value, false, [.. results.Select(r => r.Node)]));
            }

            case ThresholdExpression th:
            {
                // Every operand is evaluated: the test needs both the definite and the possible true count, so a
                // threshold never short-circuits. The cost is linear in the operand count.
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateThreshold(th.Comparison, th.K, [.. results.Select(r => r.Value)]);
                string description = $"{shape.OpName}({shape.K})";
                return new EvalResult(value, new TraceNode(description, value, false, [.. results.Select(r => r.Node)]));
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
        return new EvalResult(value, new TraceNode("If", value, false, [condition.Node, whenTrue.Node, whenFalse.Node]));
    }

    /// <summary>Records <paramref name="node"/> as not evaluated in the trace and trace tree.</summary>
    private EvalResult Skip(Expression node)
    {
        string skippedDescription = NodeText(node);
        this.trace.Add(new TraceEntry(skippedDescription, null, true));
        return new EvalResult(TruthValue.Unknown, new TraceNode(skippedDescription, null, true, []));
    }

    /// <summary>
    /// Folds the operands of an <c>AND</c> or <c>OR</c> chain from left to right, starting from <paramref name="identity"/>
    /// (<c>True</c> for <c>AND</c>, <c>False</c> for <c>OR</c>). Once an operand produces a value for which
    /// <paramref name="stops"/> holds, the rest are skipped and recorded as <c>NotEvaluated</c>, unless
    /// <see cref="EvaluationMode.Exhaustive"/> is on. The fold is the Strong Kleene minimum or maximum, so stopping never
    /// changes the result.
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
        List<TraceNode> children = new(operands.Count);
        bool exhaustive = this.options.Mode == EvaluationMode.Exhaustive;
        bool stop = false;
        foreach (Expression operand in operands)
        {
            if (stop || this.aborted)
            {
                string skippedDescription = NodeText(operand);
                this.trace.Add(new TraceEntry(skippedDescription, null, true));
                children.Add(new TraceNode(skippedDescription, null, true, []));
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

        return new EvalResult(accumulator, new TraceNode(description, accumulator, false, children));
    }

    private async ValueTask<IReadOnlyList<EvalResult>> EvalAllAsync(IReadOnlyList<Expression> operands)
    {
        List<EvalResult> values = new(operands.Count);
        foreach (Expression operand in operands)
        {
            values.Add(await this.EvalAsync(operand).ConfigureAwait(false));
        }

        return values;
    }

    private async ValueTask<EvalResult> EvalTermAsync(TermExpression term)
    {
        string description = term.Identity.ToString();
        if (term.IsUnknownPredicate)
        {
            this.trace.Add(new TraceEntry(description, TruthValue.Unknown, false));
            return new EvalResult(TruthValue.Unknown, new TraceNode(description, TruthValue.Unknown, false, []));
        }

        if (this.memo.TryGetValue(term.Identity, out TruthValue cached))
        {
            // A repeat of a term that resolved earlier shows the same values as its first occurrence.
            description = this.valueDescriptions.GetValueOrDefault(term.Identity, description);
            this.trace.Add(new TraceEntry(description, cached, false));
            return new EvalResult(cached, new TraceNode(description, cached, false, []));
        }

        TruthValue result = await this.InvokeAsync(term.Identity).ConfigureAwait(false);
        this.memo[term.Identity] = result;
        description = this.valueDescriptions.GetValueOrDefault(term.Identity, description);
        this.trace.Add(new TraceEntry(description, result, false));
        return new EvalResult(result, new TraceNode(description, result, false, []));
    }

    private async ValueTask<TruthValue> InvokeAsync(TermIdentity identity)
    {
        if (!this.registry.TryGet(identity.PredicateName, out PredicateDescriptor<TContext>? descriptor))
        {
            // Defensive only: a successfully Strict-mode-compiled rule cannot reference an
            // unregistered predicate, so this path is unreachable in practice.
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
                this.valueDescriptions[identity] = DescribeWithValues(identity, values);
            }

            PredicateArguments args = new(values);

            // A predicate may answer Unknown directly; that is a normal value, not a fault. Only a throw
            // (including a timeout or cancellation surfaced as an exception) is recorded as a Fault.
            return descriptor.Evaluate is { } lambda
                ? await lambda(this.context, args, this.cancellationToken).ConfigureAwait(false)
                : await this.InvokeClassBasedAsync(descriptor, args).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !this.cancellationToken.IsCancellationRequested)
        {
            this.RecordFault(identity, ex);
            return TruthValue.Unknown;
        }
    }

    /// <summary>Records a fault for <paramref name="identity"/> and aborts the evaluation once the fault budget is exceeded.</summary>
    private void RecordFault(TermIdentity identity, Exception ex)
    {
        this.faults.Add(new Fault(identity, ex));
        EvaluationLog.PredicateFaulted(this.logger, identity.ToString(), ex.Message, ex);
        TruthWeaverMetrics.FaultRecorded();

        // Ticket 11's acceptance criteria (FaultBudget = 1 tolerates the first fault and aborts
        // on the second) takes precedence over ADR-0002's own prose example (which reads as
        // "budget = 1 aborts on the first fault") — the ticket is the more operationally precise
        // of the two, so a fault count strictly greater than the budget is what triggers an abort.
        if (this.options.FaultBudget is { } budget && this.faults.Count > budget)
        {
            this.aborted = true;
        }
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
            }
            else
            {
                failure = new VariableResolutionException(reference, kind, $"{reference}: {message}");
            }

            if (failure is not null)
            {
                resolved = false;
                this.RecordFault(identity, failure);
            }
        }

        return resolved;
    }

    /// <summary>Asks the named source a query, once per evaluation; a failure is stored and replayed like a value.</summary>
    private async ValueTask<SourceAnswer> QuerySourceAsync(VariableReference reference)
    {
        if (this.sourceAnswers.TryGetValue(reference, out SourceAnswer? known))
        {
            return known;
        }

        SourceAnswer answer = await this.QuerySourceUncachedAsync(reference).ConfigureAwait(false);
        this.sourceAnswers[reference] = answer;
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
    /// <see cref="Expression"/> is recursed over — every recursive evaluation step returns one of these
    /// instead of a bare <see cref="TruthValue"/>, so the per-node annotations needed for
    /// <see cref="Decision.TraceTree"/> fall out of the existing recursion for free, with no
    /// separate replay pass.
    /// </summary>
    private readonly record struct EvalResult(TruthValue Value, TraceNode Node);

    /// <summary>What a data source answered to one reference: its matches, or the failure to replay.</summary>
    private sealed record SourceAnswer(IReadOnlyList<LiteralValue> Matches, VariableResolutionException? Failure)
    {
        public static SourceAnswer Answered(IReadOnlyList<LiteralValue> matches)
        {
            return new(matches, null);
        }

        public static SourceAnswer Failed(VariableResolutionException failure)
        {
            return new([], failure);
        }
    }
}
