namespace TruthWeaver.Testing;

using System.Globalization;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Generates random valid rules over the predicates of a registry and checks each rule against a brute-force Strong Kleene
/// (K3) oracle. Use it to confirm that the engine handles the predicate schemas of an application.
/// </summary>
/// <remarks>
/// <para>
/// The fuzzer reads only the schemas of the registry. It compiles each rule against a stand-in registry with the same
/// schemas, in which each predicate returns the truth value that the current assignment gives it. Thus the real
/// predicates never run, and a predicate that needs services, data or a context is still fuzzable.
/// </para>
/// <para>
/// Each predicate becomes one term. The fuzzer gives each required argument a typical value of its kind and omits each
/// optional argument, so the compiler supplies its default. A predicate whose call does not compile with these values is
/// listed in <see cref="RuleFuzzReport.SkippedPredicates"/>.
/// </para>
/// </remarks>
public static class RuleFuzzer
{
    /// <summary>
    /// Generates rules over the predicates of <paramref name="registry"/> and runs every <see cref="RuleFuzzCheck"/> on
    /// each rule that compiles.
    /// </summary>
    /// <typeparam name="TContext">The application context type of the registry. The fuzzer never creates a context.</typeparam>
    /// <param name="registry">The registry whose predicate schemas the rules use.</param>
    /// <param name="seed">The seed of the random source. The same seed, schemas and options give the same rules.</param>
    /// <param name="options">The size of the run, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token that stops the run.</param>
    /// <returns>The report. Call <see cref="RuleFuzzReport.ShouldPass"/> to fail a test on a failed check.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The registry has no predicate that the fuzzer can call.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of its range.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is cancelled.</exception>
    public static Task<RuleFuzzReport> RunAsync<TContext>(
        PredicateRegistry<TContext> registry,
        int seed,
        RuleFuzzerOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(registry);
        return RunAsync(
            registry.Schemas,
            seed,
            options ?? new RuleFuzzerOptions(),
            static (rule, _) => rule.Simplify(),
            cancellationToken
        );
    }

    /// <summary>Runs the fuzzer with a replaceable <c>Simplify</c> step, so that tests can force a failed check.</summary>
    /// <param name="schemas">The predicate schemas.</param>
    /// <param name="seed">The seed of the random source.</param>
    /// <param name="options">The size of the run.</param>
    /// <param name="simplify">
    /// The rewrite that the <see cref="RuleFuzzCheck.Simplify"/> check verifies. It receives the rule and the stand-in compiler.
    /// </param>
    /// <param name="cancellationToken">A token that stops the run.</param>
    /// <returns>The report.</returns>
    internal static async Task<RuleFuzzReport> RunAsync(
        IEnumerable<PredicateSchema> schemas,
        int seed,
        RuleFuzzerOptions options,
        Func<CompiledRule<FuzzAssignment>, RuleCompiler<FuzzAssignment>, CompiledRule<FuzzAssignment>> simplify,
        CancellationToken cancellationToken
    )
    {
        if (options.RuleCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.RuleCount, "RuleCount must be one or more.");
        }

        if (options.MaxDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxDepth, "MaxDepth must not be negative.");
        }

        if (options.MaxTerms < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxTerms, "MaxTerms must be one or more.");
        }

        // Registry order is not guaranteed, so sort by name to make the seed reproduce the run.
        PredicateSchema[] ordered = [.. schemas.OrderBy(s => s.Name, StringComparer.Ordinal)];
        RuleCompiler<FuzzAssignment> compiler = new(StandInRegistry(ordered));

        List<FuzzTerm> terms = [];
        List<string> skipped = [];
        foreach (PredicateSchema schema in ordered)
        {
            if (TermText(schema, compiler) is { } text)
            {
                terms.Add(new FuzzTerm(schema.Name, text));
            }
            else
            {
                skipped.Add(schema.Name);
            }
        }

        if (terms.Count == 0)
        {
            throw new ArgumentException(
                "The registry has no predicate that the fuzzer can call with generated arguments.",
                nameof(schemas)
            );
        }

        Random random = new(seed);
        int termsPerRule = Math.Min(options.MaxTerms, terms.Count);
        List<RuleFuzzFailure> failures = [];
        int rulesChecked = 0;
        for (int index = 0; index < options.RuleCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FuzzTerm[] picked = Pick(random, terms, termsPerRule);
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, options.MaxDepth, [.. picked.Select(t => t.Text)]);
            if (compiler.Compile(generated.Text).CompiledRule is not { } rule)
            {
                // The generator can write an authoring error, such as an out-of-range threshold. It is not an engine fault.
                continue;
            }

            rulesChecked++;
            FuzzCase fuzzCase = new(seed, index, generated, picked);
            await AddFailureAsync(failures, fuzzCase, RuleFuzzCheck.Evaluation, rule, cancellationToken).ConfigureAwait(false);
            CompiledRule<FuzzAssignment> simplified = simplify(rule, compiler);
            CompiledRule<FuzzAssignment> canonical = rule.Canonicalize();
            await AddFailureAsync(failures, fuzzCase, RuleFuzzCheck.Simplify, simplified, cancellationToken)
                .ConfigureAwait(false);
            await AddFailureAsync(failures, fuzzCase, RuleFuzzCheck.Canonicalize, canonical, cancellationToken)
                .ConfigureAwait(false);
            AddSizeFailure(failures, fuzzCase, RuleFuzzCheck.SimplifyNeverLarger, rule, simplified);
            AddIdempotenceFailure(
                failures,
                fuzzCase,
                RuleFuzzCheck.SimplifyIdempotent,
                simplified,
                simplify(simplified, compiler)
            );
            AddSizeFailure(failures, fuzzCase, RuleFuzzCheck.CanonicalizeNeverLarger, rule, canonical);
            AddIdempotenceFailure(
                failures,
                fuzzCase,
                RuleFuzzCheck.CanonicalizeIdempotent,
                canonical,
                canonical.Canonicalize()
            );
            await AddNormalFormFailuresAsync(failures, fuzzCase, rule, cancellationToken).ConfigureAwait(false);
            AddRoundTripFailure(failures, fuzzCase, RuleFuzzCheck.DslRoundTrip, rule, compiler.Compile(rule.CanonicalText));
            AddRoundTripFailure(failures, fuzzCase, RuleFuzzCheck.JsonRoundTrip, rule, compiler.CompileJson(rule.PrintJson()));
        }

        return new RuleFuzzReport(seed, [.. terms.Select(t => t.Text)], skipped, options.RuleCount, rulesChecked, failures);
    }

    /// <summary>Builds a registry with the same schemas, in which each predicate returns its value from the assignment.</summary>
    private static PredicateRegistry<FuzzAssignment> StandInRegistry(IEnumerable<PredicateSchema> schemas)
    {
        PredicateRegistryBuilder<FuzzAssignment> builder = PredicateRegistry<FuzzAssignment>.CreateBuilder();
        foreach (PredicateSchema schema in schemas)
        {
            string name = schema.Name;
            builder.Add(schema, (assignment, _, _) => ValueTask.FromResult(assignment.ValueOf(name)));
        }

        return builder.Build();
    }

    /// <summary>Gets the canonical text of a call of the predicate, or <see langword="null"/> when the call does not compile.</summary>
    private static string? TermText(PredicateSchema schema, RuleCompiler<FuzzAssignment> compiler)
    {
        (string Name, object Value)[] arguments =
        [
            .. schema.Arguments.Where(a => a.Required).Select(a => (a.Name, ToObject(HarnessValues.Typical(a.Type)))),
        ];
        return RuleBuilder.Predicate(schema.Name, arguments).Compile(compiler).CompiledRule?.CanonicalText;
    }

    /// <summary>Converts a literal to the CLR value that <c>RuleBuilder.Predicate</c> takes.</summary>
    private static object ToObject(LiteralValue value)
    {
        return value.Kind switch
        {
            LiteralKind.String => value.AsString(),
            LiteralKind.Int64 => value.AsInt64(),
            LiteralKind.Decimal => value.AsDecimal(),
            LiteralKind.Boolean => value.AsBoolean(),
            LiteralKind.DateTimeOffset => value.AsDateTimeOffset(),
            LiteralKind.Guid => value.AsGuid(),
            _ => value.AsArray().Select(ToObject).ToArray(),
        };
    }

    /// <summary>Picks <paramref name="count"/> distinct terms with a partial Fisher-Yates shuffle.</summary>
    private static FuzzTerm[] Pick(Random random, List<FuzzTerm> terms, int count)
    {
        FuzzTerm[] pool = [.. terms];
        for (int i = 0; i < count; i++)
        {
            int j = random.Next(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool[..count];
    }

    /// <summary>
    /// Evaluates <paramref name="candidate"/> for every assignment and adds a failure at the first assignment where its value
    /// differs from the oracle value of the generated rule.
    /// </summary>
    private static async Task AddFailureAsync(
        List<RuleFuzzFailure> failures,
        FuzzCase fuzzCase,
        RuleFuzzCheck check,
        CompiledRule<FuzzAssignment> candidate,
        CancellationToken cancellationToken
    )
    {
        foreach (TruthValue[] values in K3Oracle.Assignments(fuzzCase.Terms.Count))
        {
            Dictionary<string, TruthValue> byName = [with(StringComparer.Ordinal)];
            for (int i = 0; i < values.Length; i++)
            {
                byName[fuzzCase.Terms[i].PredicateName] = values[i];
            }

            Decision decision = await candidate
                .EvaluateAsync(new FuzzAssignment(byName), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            TruthValue expected = fuzzCase.Generated.Eval(values);
            if (decision.Result == expected)
            {
                continue;
            }

            string assignment = string.Join(", ", fuzzCase.Terms.Select((t, i) => $"{t.Text} = {values[i]}"));
            string subject =
                check == RuleFuzzCheck.Evaluation ? "the evaluator returns" : $"'{candidate.CanonicalText}' returns";
            failures.Add(
                fuzzCase.Failure(check, $"With {assignment}, {subject} {decision.Result}, but Strong Kleene gives {expected}.")
            );
            return;
        }
    }

    /// <summary>Adds a failure when the rewritten rule has more nodes than the original.</summary>
    private static void AddSizeFailure(
        List<RuleFuzzFailure> failures,
        FuzzCase fuzzCase,
        RuleFuzzCheck check,
        CompiledRule<FuzzAssignment> original,
        CompiledRule<FuzzAssignment> rewritten
    )
    {
        if (rewritten.Metrics.NodeCount > original.Metrics.NodeCount)
        {
            failures.Add(
                fuzzCase.Failure(
                    check,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The rule has {original.Metrics.NodeCount} nodes and '{rewritten.CanonicalText}' has {rewritten.Metrics.NodeCount} nodes."
                    )
                )
            );
        }
    }

    /// <summary>
    /// Checks <c>ToNnf</c>, <c>ToCnf</c> and <c>ToDnf</c>: the value for every assignment, and idempotence. A rule whose form
    /// is over the rewrite size cap has no result and is not checked, because the cap is a documented refusal.
    /// </summary>
    private static async Task AddNormalFormFailuresAsync(
        List<RuleFuzzFailure> failures,
        FuzzCase fuzzCase,
        CompiledRule<FuzzAssignment> rule,
        CancellationToken cancellationToken
    )
    {
        (
            RuleFuzzCheck Value,
            RuleFuzzCheck Idempotent,
            Func<CompiledRule<FuzzAssignment>, CompilationResult<FuzzAssignment>> Rewrite
        )[] forms =
        [
            (RuleFuzzCheck.ToNnf, RuleFuzzCheck.NnfIdempotent, static r => r.ToNnf()),
            (RuleFuzzCheck.ToCnf, RuleFuzzCheck.CnfIdempotent, static r => r.ToCnf()),
            (RuleFuzzCheck.ToDnf, RuleFuzzCheck.DnfIdempotent, static r => r.ToDnf()),
        ];
        foreach (
            (
                RuleFuzzCheck value,
                RuleFuzzCheck idempotent,
                Func<CompiledRule<FuzzAssignment>, CompilationResult<FuzzAssignment>> rewrite
            ) in forms
        )
        {
            if (rewrite(rule).CompiledRule is not { } once)
            {
                continue;
            }

            await AddFailureAsync(failures, fuzzCase, value, once, cancellationToken).ConfigureAwait(false);
            if (rewrite(once).CompiledRule is { } twice)
            {
                AddIdempotenceFailure(failures, fuzzCase, idempotent, once, twice);
            }
        }
    }

    /// <summary>Adds a failure when a second rewrite changes the canonical text of the first result.</summary>
    private static void AddIdempotenceFailure(
        List<RuleFuzzFailure> failures,
        FuzzCase fuzzCase,
        RuleFuzzCheck check,
        CompiledRule<FuzzAssignment> once,
        CompiledRule<FuzzAssignment> twice
    )
    {
        if (twice.CanonicalText != once.CanonicalText)
        {
            failures.Add(fuzzCase.Failure(check, $"Rewriting '{once.CanonicalText}' again gives '{twice.CanonicalText}'."));
        }
    }

    /// <summary>Adds a failure when the recompiled rule fails to compile or prints a different canonical text.</summary>
    private static void AddRoundTripFailure(
        List<RuleFuzzFailure> failures,
        FuzzCase fuzzCase,
        RuleFuzzCheck check,
        CompiledRule<FuzzAssignment> rule,
        CompilationResult<FuzzAssignment> recompiled
    )
    {
        if (recompiled.CompiledRule is not { } copy)
        {
            failures.Add(fuzzCase.Failure(check, $"The printed form does not compile: {recompiled.FormatDiagnostics()}"));
        }
        else if (copy.CanonicalText != rule.CanonicalText)
        {
            failures.Add(
                fuzzCase.Failure(
                    check,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The printed form compiles to '{copy.CanonicalText}', not '{rule.CanonicalText}'."
                    )
                )
            );
        }
    }

    /// <summary>One predicate of the run.</summary>
    /// <param name="PredicateName">The registered name, the key of the assignment.</param>
    /// <param name="Text">The canonical text of the call, as it appears in a generated rule.</param>
    private sealed record FuzzTerm(string PredicateName, string Text);

    /// <summary>One generated rule and the facts that a failure reports.</summary>
    /// <param name="Seed">The seed of the run.</param>
    /// <param name="Index">The position of the rule in the run.</param>
    /// <param name="Generated">The generated rule.</param>
    /// <param name="Terms">The terms of the rule, in the order of the assignment.</param>
    private sealed record FuzzCase(int Seed, int Index, GeneratedRule Generated, IReadOnlyList<FuzzTerm> Terms)
    {
        public RuleFuzzFailure Failure(RuleFuzzCheck check, string detail)
        {
            return new RuleFuzzFailure(this.Seed, this.Index, this.Generated.Text, check, detail);
        }
    }
}
