namespace TruthWeaver.Tests.PredicateCatalog;

using System.Reflection;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Checks the <c>NotX</c> twin rule of the predicate catalog. Every public predicate factory of the predicates package is
/// either half of a twin pair or listed with a reason for having no twin. For each pair, the twin answers
/// <c>NOT positive</c>, computed by the engine, for a selected value that makes the positive factory True, False and
/// Unknown, so a twin keeps Unknown.
/// </summary>
/// <remarks>
/// A factory key is <c>Class.Method(Selector)</c>: the factory class, the method name and the selected type (the element
/// type for a nullable value type). Overloads that differ only by selected type get distinct keys. A factory with no
/// <c>selector</c> parameter is <c>Class.Method&lt;T1, T2&gt;</c>, by its type parameters. Failures are returned, not
/// thrown, so fixtures can prove the checks fail.
/// </remarks>
internal static class NotXTwinChecker
{
    private const string PositiveName = "positive";

    private const string TwinName = "twin";

    /// <summary>Lists the key of every public predicate factory of the predicates package.</summary>
    /// <returns>The factory keys, in ordinal order, without duplicates.</returns>
    internal static IReadOnlyList<string> CatalogFactories()
    {
        return [.. Keys(_ => true)];
    }

    /// <summary>Lists the key of every public predicate factory that takes a <c>nullBehavior</c> option.</summary>
    /// <returns>The factory keys, in ordinal order, without duplicates.</returns>
    internal static IReadOnlyList<string> NullBehaviorFactories()
    {
        return [.. Keys(method => method.GetParameters().Any(parameter => parameter.Name == "nullBehavior"))];
    }

    /// <summary>Checks that the table accounts for every factory exactly once and names no other factory.</summary>
    /// <param name="factories">The factory keys of the catalog.</param>
    /// <param name="table">The reviewed twin table.</param>
    /// <returns>One message per failure; empty when the table and the catalog agree.</returns>
    internal static IReadOnlyList<string> CheckCoverage(
        IReadOnlyCollection<string> factories,
        IReadOnlyList<TwinTableEntry> table
    )
    {
        List<string> failures = [];
        Dictionary<string, int> listed = new(StringComparer.Ordinal);
        foreach (string key in table.SelectMany(entry => entry.Factories))
        {
            listed[key] = listed.GetValueOrDefault(key) + 1;
        }

        foreach (string factory in factories.Order(StringComparer.Ordinal))
        {
            if (!listed.ContainsKey(factory))
            {
                failures.Add(
                    $"'{factory}' is a predicate factory with no NotX twin in the twin table. Add the twin to the catalog and a "
                        + "pair to NotXTwinTable, or list the factory there as having no twin and say why."
                );
            }
        }

        foreach ((string key, int count) in listed.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!factories.Contains(key, StringComparer.Ordinal))
            {
                failures.Add($"The twin table names '{key}', which is not a predicate factory of the predicates package.");
            }
            else if (count > 1)
            {
                failures.Add($"The twin table names '{key}' {count} times. Each factory belongs to one row.");
            }
        }

        return failures;
    }

    /// <summary>
    /// Checks that every pair whose factories take a <c>nullBehavior</c> option states its null cases, so the default and
    /// the <see cref="NullBehavior.False"/> registrations are checked for each such pair.
    /// </summary>
    /// <param name="nullBehaviorFactories">The keys of the factories that take a <c>nullBehavior</c> option.</param>
    /// <param name="pairs">The pairs of the reviewed twin table.</param>
    /// <returns>One message per pair with no null cases; empty when every such pair states them.</returns>
    internal static IReadOnlyList<string> CheckNullCaseCoverage(
        IReadOnlyCollection<string> nullBehaviorFactories,
        IEnumerable<TwinPair> pairs
    )
    {
        return
        [
            .. pairs
                .Where(pair =>
                    pair.Nulls is null
                    && (
                        nullBehaviorFactories.Contains(pair.Positive, StringComparer.Ordinal)
                        || nullBehaviorFactories.Contains(pair.Twin, StringComparer.Ordinal)
                    )
                )
                .Select(pair =>
                    $"{pair.Positive}: the pair takes a nullBehavior option, but its twin table row states no null cases. "
                    + "Add Nulls with the default and the NullBehavior.False registrations."
                ),
        ];
    }

    /// <summary>
    /// Checks one pair: each probe makes the positive factory give the answer the table claims, and the twin then answers
    /// what the engine's K3 <c>NOT</c> of the positive factory answers. When the pair states its null cases, the twin also
    /// answers <c>NOT positive</c> for a null selected value with both registered by default and with both registered
    /// with <see cref="NullBehavior.False"/>, so a twin whose default differs from its positive's is reported.
    /// </summary>
    /// <param name="pair">The pair to check.</param>
    /// <param name="cancellationToken">Cancels the evaluations.</param>
    /// <returns>One message per failure; empty when the twin is the K3 complement for every probe.</returns>
    internal static async Task<IReadOnlyList<string>> CheckPairAsync(TwinPair pair, CancellationToken cancellationToken)
    {
        List<string> failures = [];
        PredicateRegistryBuilder<TwinProbeContext> registry = PredicateRegistry<TwinProbeContext>.CreateBuilder();
        Register(registry, pair.PositiveFactory, PositiveName);
        Register(registry, pair.TwinFactory, TwinName);
        RuleCompiler<TwinProbeContext> compiler = new(registry.Build());

        // NOT positive is compiled as a rule, so the complement is the engine's own K3 negation.
        RuleBuilder positiveTerm = RuleBuilder.Predicate(PositiveName, pair.Arguments);
        CompiledRule<TwinProbeContext>?[] rules =
        [
            Compile(compiler, positiveTerm, pair.Positive, failures),
            Compile(compiler, RuleBuilder.Not(positiveTerm), $"NOT {pair.Positive}", failures),
            Compile(compiler, RuleBuilder.Predicate(TwinName, pair.Arguments), pair.Twin, failures),
        ];
        if (rules is not [{ } positiveRule, { } negatedRule, { } twinRule])
        {
            return failures;
        }

        (TruthValue Expected, TwinProbeContext? Context)[] probes =
        [
            (TruthValue.True, pair.WhenTrue),
            (TruthValue.False, pair.WhenFalse),
            (TruthValue.Unknown, pair.WhenUnknown),
        ];
        foreach ((TruthValue expected, TwinProbeContext? context) in probes)
        {
            if (context is null)
            {
                if (string.IsNullOrWhiteSpace(pair.Unreachable))
                {
                    failures.Add($"{pair.Positive}: the twin table has no {expected} probe and does not say why.");
                }

                continue;
            }

            string probe = expected.ToString();
            TruthValue? answer = await EvaluateAsync(positiveRule, context, pair.Positive, probe, failures, cancellationToken);
            TruthValue? negated = await EvaluateAsync(negatedRule, context, pair.Positive, probe, failures, cancellationToken);
            TruthValue? twinAnswer = await EvaluateAsync(twinRule, context, pair.Twin, probe, failures, cancellationToken);
            if (answer is null || negated is null || twinAnswer is null)
            {
                continue;
            }

            if (answer != expected)
            {
                failures.Add($"{pair.Positive}: the {expected} probe answers {answer}. Fix the probe in the twin table.");
            }
            else if (twinAnswer != negated)
            {
                string keep = expected == TruthValue.Unknown ? " A twin keeps Unknown as Unknown." : string.Empty;
                failures.Add(
                    $"{pair.Twin}: for the {expected} probe it answers {twinAnswer}, but NOT {pair.Positive} answers {negated}.{keep}"
                );
            }
        }

        if (pair.Nulls is { } nulls)
        {
            await CheckNullCaseAsync(
                pair,
                "registered with no NullBehavior",
                nulls.DefaultPositive,
                nulls.DefaultTwin,
                failures,
                cancellationToken
            );
            await CheckNullCaseAsync(
                pair,
                "registered with NullBehavior.False",
                nulls.FalsePositive,
                nulls.FalseTwin,
                failures,
                cancellationToken
            );
        }

        return failures;
    }

    /// <summary>
    /// Checks that the twin answers what the engine's K3 <c>NOT</c> of the positive factory answers for a null selected
    /// value, with both factories registered the same way.
    /// </summary>
    private static async Task CheckNullCaseAsync(
        TwinPair pair,
        string registration,
        ProbeFactory positiveFactory,
        ProbeFactory twinFactory,
        List<string> failures,
        CancellationToken cancellationToken
    )
    {
        PredicateRegistryBuilder<TwinProbeContext> registry = PredicateRegistry<TwinProbeContext>.CreateBuilder();
        Register(registry, positiveFactory, PositiveName);
        Register(registry, twinFactory, TwinName);
        RuleCompiler<TwinProbeContext> compiler = new(registry.Build());
        RuleBuilder positiveTerm = RuleBuilder.Predicate(PositiveName, pair.Arguments);
        CompiledRule<TwinProbeContext>? negatedRule = Compile(
            compiler,
            RuleBuilder.Not(positiveTerm),
            $"NOT {pair.Positive} {registration}",
            failures
        );
        CompiledRule<TwinProbeContext>? twinRule = Compile(
            compiler,
            RuleBuilder.Predicate(TwinName, pair.Arguments),
            $"{pair.Twin} {registration}",
            failures
        );
        if (negatedRule is null || twinRule is null)
        {
            return;
        }

        // Every property of an empty context is null, so each factory selects a null value.
        TwinProbeContext missing = new();
        string probe = $"null selected value {registration}";
        TruthValue? negated = await EvaluateAsync(negatedRule, missing, pair.Positive, probe, failures, cancellationToken);
        TruthValue? twinAnswer = await EvaluateAsync(twinRule, missing, pair.Twin, probe, failures, cancellationToken);
        if (negated is not null && twinAnswer is not null && twinAnswer != negated)
        {
            failures.Add(
                $"{pair.Twin}: for a {probe} it answers {twinAnswer}, but NOT {pair.Positive} answers {negated}. "
                    + "A twin has the same default NullBehavior as its positive."
            );
        }
    }

    private static void Register(PredicateRegistryBuilder<TwinProbeContext> registry, ProbeFactory factory, string name)
    {
        (
            PredicateSchema schema,
            Func<TwinProbeContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
        ) = factory(name);
        registry.Add(schema, evaluate);
    }

    private static CompiledRule<TwinProbeContext>? Compile(
        RuleCompiler<TwinProbeContext> compiler,
        RuleBuilder rule,
        string description,
        List<string> failures
    )
    {
        CompilationResult<TwinProbeContext> result = rule.Compile(compiler);
        if (result.CompiledRule is null)
        {
            failures.Add($"{description}: the probe rule does not compile. {result.FormatDiagnostics()}");
        }

        return result.CompiledRule;
    }

    private static async Task<TruthValue?> EvaluateAsync(
        CompiledRule<TwinProbeContext> rule,
        TwinProbeContext context,
        string factory,
        string probe,
        List<string> failures,
        CancellationToken cancellationToken
    )
    {
        Decision decision = await rule.EvaluateAsync(
            context,
            EmptyServiceProvider.Instance,
            cancellationToken: cancellationToken
        );
        if (decision.Faults.Count == 0)
        {
            return decision.Result;
        }

        failures.Add($"{factory}: the {probe} probe faulted: {decision.Faults[0].Exception.Message}");
        return null;
    }

    private static IEnumerable<string> Keys(Func<MethodInfo, bool> include)
    {
        return typeof(StringPredicates)
            .Assembly.GetExportedTypes()
            .Where(type =>
                type is { IsAbstract: true, IsSealed: true, Namespace: "TruthWeaver.Predicates" }
                && type.Name.EndsWith("Predicates", StringComparison.Ordinal)
            )
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(method => method.IsGenericMethodDefinition && method.ReturnType.IsValueType && include(method))
                    .Select(method => $"{type.Name}.{method.Name}{Signature(method)}")
            )
            .Distinct()
            .Order(StringComparer.Ordinal);
    }

    private static string Signature(MethodInfo method)
    {
        ParameterInfo? selector = method.GetParameters().FirstOrDefault(parameter => parameter.Name == "selector");
        if (selector is null)
        {
            return $"<{string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name))}>";
        }

        // The selector is Func<TContext, TSelected>; the selected type tells the overloads apart.
        Type selected = selector.ParameterType.GetGenericArguments()[1];
        return $"({TypeName(Nullable.GetUnderlyingType(selected) ?? selected)})";
    }

    private static string TypeName(Type type)
    {
        return type.IsGenericType
            ? $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
            : type.Name;
    }
}
