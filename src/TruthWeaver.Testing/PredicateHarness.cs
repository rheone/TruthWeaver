namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>
/// Runs standard checks against a real predicate and reports what held: a deterministic answer, robust handling of
/// boundary argument values, conformance to the declared schema, and the response to cancellation.
/// </summary>
/// <remarks>
/// <para>
/// The harness calls the evaluation delegate directly, with the same <see cref="PredicateArguments"/> type the engine
/// passes. It judges each call the way the evaluator does: a returned value is the term's value, and a thrown exception
/// becomes <see cref="TruthValue.Unknown"/> plus a <see cref="Fault"/>. An <see cref="TruthValue.Unknown"/> answer is
/// valid. A thrown exception is a failure unless <see cref="PredicateHarnessOptions.ExpectedFaults"/> allows it, because
/// in production it turns into a fault on every call with that value.
/// </para>
/// <para>
/// The harness reports cancellation and never enforces it. The evaluator checks its token before each predicate call,
/// so a predicate that ignores the token still works; it only keeps running longer than it must.
/// </para>
/// </remarks>
public static class PredicateHarness
{
    /// <summary>Runs every harness check against one predicate and returns the report.</summary>
    /// <typeparam name="TContext">The application context type that the predicate reads.</typeparam>
    /// <param name="schema">The predicate's schema. The boundary values and the conformance check come from its arguments.</param>
    /// <param name="evaluate">
    /// The evaluation delegate, the same shape that <c>PredicateRegistryBuilder.Add</c> takes. For a class-based predicate,
    /// pass <c>TPredicate.Schema</c> and the instance's <see cref="IPredicate{TContext}.EvaluateAsync"/> method.
    /// </param>
    /// <param name="context">The context for every call.</param>
    /// <param name="options">The baseline arguments and expected faults, or <see langword="null"/> for <see cref="PredicateHarnessOptions.Default"/>.</param>
    /// <param name="cancellationToken">A token that stops the run. The harness passes it to the predicate for every check except cancellation.</param>
    /// <returns>The report, with one outcome per check or check case.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> or <paramref name="evaluate"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is cancelled.</exception>
    public static async Task<PredicateHarnessReport> RunAsync<TContext>(
        PredicateSchema schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate,
        TContext context,
        PredicateHarnessOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(evaluate);
        options ??= PredicateHarnessOptions.Default;

        // One set collects the argument names read by every call except the cancellation call, so the conformance
        // check sees the union of reads over the baseline and every boundary value.
        HashSet<string> reads = new(StringComparer.Ordinal);
        Dictionary<string, LiteralValue> baseline = BaselineArguments(schema, options);
        List<HarnessCase> cases = [new("baseline arguments", null, baseline), .. BoundaryCases(schema, baseline)];

        List<PredicateHarnessOutcome> outcomes = [];
        List<string> differences = [];
        foreach (HarnessCase harnessCase in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallResult first = await CallAsync(evaluate, context, harnessCase, reads, cancellationToken).ConfigureAwait(false);
            CallResult second = await CallAsync(evaluate, context, harnessCase, reads, cancellationToken).ConfigureAwait(false);
            if (!first.SameAnswer(second))
            {
                differences.Add($"{harnessCase.Name} gave {first}, then {second}");
            }

            outcomes.Add(CallOutcome(harnessCase, first, options));
        }

        outcomes.Add(
            differences.Count == 0
                ? new(
                    PredicateHarnessCheck.Determinism,
                    PredicateHarnessStatus.Passed,
                    "all argument sets",
                    $"Two calls gave the same answer for each of {cases.Count} argument sets."
                )
                : new(
                    PredicateHarnessCheck.Determinism,
                    PredicateHarnessStatus.Failed,
                    "all argument sets",
                    $"Two calls with the same arguments and context gave different answers: {string.Join("; ", differences)}."
                )
        );
        outcomes.AddRange(SchemaOutcomes(schema, reads));
        outcomes.Add(await CancellationOutcomeAsync(evaluate, context, baseline).ConfigureAwait(false));
        return new PredicateHarnessReport(schema.Name, outcomes);
    }

    private static Dictionary<string, LiteralValue> BaselineArguments(PredicateSchema schema, PredicateHarnessOptions options)
    {
        Dictionary<string, LiteralValue> baseline = new(StringComparer.Ordinal);
        foreach (PredicateArgumentSchema argument in schema.Arguments)
        {
            baseline[argument.Name] = argument.Default ?? HarnessValues.Typical(argument.Type);
        }

        // Author values win over generated ones, and an author value for an undeclared name is passed as given.
        foreach ((string name, LiteralValue value) in options.Arguments)
        {
            baseline[name] = value;
        }

        return baseline;
    }

    private static IEnumerable<HarnessCase> BoundaryCases(PredicateSchema schema, Dictionary<string, LiteralValue> baseline)
    {
        foreach (PredicateArgumentSchema argument in schema.Arguments)
        {
            foreach ((string label, LiteralValue value) in HarnessValues.Boundaries(argument.Type))
            {
                Dictionary<string, LiteralValue> values = new(baseline, StringComparer.Ordinal) { [argument.Name] = value };
                yield return new($"{argument.Name} = {label}", argument.Name, values);
            }

            // The compiler fills in a default, so only an optional argument without a default can be absent at evaluation.
            if (!argument.Required && argument.Default is null)
            {
                Dictionary<string, LiteralValue> values = new(baseline, StringComparer.Ordinal);
                values.Remove(argument.Name);
                yield return new($"{argument.Name} omitted", argument.Name, values);
            }
        }
    }

    private static async Task<CallResult> CallAsync<TContext>(
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate,
        TContext context,
        HarnessCase harnessCase,
        HashSet<string> reads,
        CancellationToken cancellationToken
    )
    {
        PredicateArguments args = new(new ArgumentReadTracker(harnessCase.Arguments, reads));
        try
        {
            TruthValue value = await evaluate(context, args, cancellationToken).ConfigureAwait(false);
            return new(value, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The same filter as the evaluator: an exception becomes Unknown plus a fault, except a cancellation of the
            // caller's own token, which stops the run.
            return new(TruthValue.Unknown, ex);
        }
    }

    private static PredicateHarnessOutcome CallOutcome(
        HarnessCase harnessCase,
        CallResult result,
        PredicateHarnessOptions options
    )
    {
        PredicateHarnessCheck check = harnessCase.ArgumentName is null
            ? PredicateHarnessCheck.Baseline
            : PredicateHarnessCheck.BoundaryValue;
        if (result.Exception is not { } exception)
        {
            string detail =
                result.Value == TruthValue.Unknown ? "Returned Unknown without a fault." : $"Returned {result.Value}.";
            return new(check, PredicateHarnessStatus.Passed, harnessCase.Name, detail, result.Value);
        }

        bool expected = options.ExpectedFaults.Any(e => e.Matches(exception, harnessCase.ArgumentName));
        return new(
            check,
            expected ? PredicateHarnessStatus.ExpectedFault : PredicateHarnessStatus.Failed,
            harnessCase.Name,
            $"Threw {(expected ? "allow-listed " : string.Empty)}{exception.GetType().Name}: {exception.Message} The engine records a Fault and the term is Unknown.",
            TruthValue.Unknown,
            exception
        );
    }

    private static IEnumerable<PredicateHarnessOutcome> SchemaOutcomes(PredicateSchema schema, HashSet<string> reads)
    {
        HashSet<string> declared = new(schema.Arguments.Select(a => a.Name), StringComparer.Ordinal);
        List<PredicateHarnessOutcome> problems = [];
        foreach (string name in schema.Arguments.Select(a => a.Name).Where(n => !reads.Contains(n)))
        {
            problems.Add(
                new(
                    PredicateHarnessCheck.SchemaConformance,
                    PredicateHarnessStatus.Failed,
                    $"argument '{name}'",
                    $"The schema declares argument '{name}', but the predicate never reads it."
                )
            );
        }

        foreach (string name in reads.Where(n => !declared.Contains(n)).Order(StringComparer.Ordinal))
        {
            problems.Add(
                new(
                    PredicateHarnessCheck.SchemaConformance,
                    PredicateHarnessStatus.Failed,
                    $"argument '{name}'",
                    $"The predicate reads argument '{name}', but the schema does not declare it."
                )
            );
        }

        return problems.Count > 0
            ? problems
            :
            [
                new(
                    PredicateHarnessCheck.SchemaConformance,
                    PredicateHarnessStatus.Passed,
                    "all arguments",
                    "The predicate reads every declared argument and no undeclared argument."
                ),
            ];
    }

    private static async Task<PredicateHarnessOutcome> CancellationOutcomeAsync<TContext>(
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate,
        TContext context,
        Dictionary<string, LiteralValue> baseline
    )
    {
        const string Case = "cancelled token";
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync().ConfigureAwait(false);

        // A separate read set: this call does not count toward schema conformance.
        PredicateArguments args = new(new ArgumentReadTracker(baseline, new HashSet<string>(StringComparer.Ordinal)));
        try
        {
            TruthValue value = await evaluate(context, args, cancelled.Token).ConfigureAwait(false);
            return new(
                PredicateHarnessCheck.Cancellation,
                PredicateHarnessStatus.Observed,
                Case,
                $"Returned {value} and did not observe the cancelled token.",
                value
            );
        }
        catch (OperationCanceledException ex)
        {
            return new(
                PredicateHarnessCheck.Cancellation,
                PredicateHarnessStatus.Observed,
                Case,
                $"Threw {ex.GetType().Name}. When the evaluation's own token is cancelled, the evaluation stops and the exception reaches the caller.",
                null,
                ex
            );
        }
        catch (Exception ex)
        {
            return new(
                PredicateHarnessCheck.Cancellation,
                PredicateHarnessStatus.Observed,
                Case,
                $"Threw {ex.GetType().Name}: {ex.Message} The engine records a Fault and the term is Unknown.",
                TruthValue.Unknown,
                ex
            );
        }
    }

    /// <summary>The value the engine records for one call, and the exception it records as a fault.</summary>
    /// <param name="Value">The answer, or <see cref="TruthValue.Unknown"/> when the predicate threw.</param>
    /// <param name="Exception">The thrown exception, or <see langword="null"/>.</param>
    private readonly record struct CallResult(TruthValue Value, Exception? Exception)
    {
        /// <summary>Two calls agree when they return the same value, or throw the same exception type.</summary>
        public bool SameAnswer(CallResult other)
        {
            return this.Value == other.Value && this.Exception?.GetType() == other.Exception?.GetType();
        }

        public override string ToString()
        {
            return this.Exception is null ? this.Value.ToString() : $"a thrown {this.Exception.GetType().Name}";
        }
    }

    /// <summary>One set of arguments to call the predicate with.</summary>
    /// <param name="Name">The case text for the report.</param>
    /// <param name="ArgumentName">The argument this case changes from the baseline, or <see langword="null"/> for the baseline.</param>
    /// <param name="Arguments">The argument values.</param>
    private sealed record HarnessCase(string Name, string? ArgumentName, IReadOnlyDictionary<string, LiteralValue> Arguments);
}
