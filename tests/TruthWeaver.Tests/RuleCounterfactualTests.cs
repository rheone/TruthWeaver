namespace TruthWeaver.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="RuleCounterfactual"/> finds the smallest sets of term changes that would give a rule a
/// target value. Decision contexts are strings such as <c>"a=T,b=F"</c>; a missing name answers <c>Unknown</c>.
/// </summary>
public sealed class RuleCounterfactualTests
{
    private static readonly string[] TermNames = ["a", "b", "c"];

    /// <summary>
    /// <c>a AND b</c> with <c>a</c> <c>False</c> and <c>b</c> <c>True</c> needs one change: <c>a</c> from <c>False</c> to
    /// <c>True</c>.
    /// </summary>
    [Fact]
    public void FindChanges_DeniedConjunction_ReturnsTheSingleTermToFix_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            Facts(("a", TruthValue.False), ("b", TruthValue.True))
        );

        Assert.Equal(CounterfactualOutcome.Found, result.Outcome);
        ChangeSet set = Assert.Single(result.ChangeSets);
        Assert.Equal([new TermChange("a", TruthValue.False, TruthValue.True)], set.Changes);
        Assert.True(result.IsComplete);
    }

    /// <summary>The alternatives come fewest changes first: one change to <c>a</c> before two changes to <c>b</c> and <c>c</c>.</summary>
    [Fact]
    public void FindChanges_TwoWaysToSatisfy_RanksTheSmallerChangeSetFirst_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(b AND c) OR a");

        CounterfactualResult result = RuleCounterfactual.FindChanges(rule, AllFalse("a", "b", "c"));

        Assert.True(result.TryGetChangeSets(out EquatableArray<ChangeSet> sets));
        Assert.Equal(["a=True", "b=True c=True"], sets.Select(Describe));
        Assert.True(result.IsComplete);
    }

    /// <summary>A fixed term is never changed, so the only way left is through the other terms.</summary>
    [Fact]
    public void FindChanges_FixedTerm_IsNeverChanged_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(b AND c) OR a");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            AllFalse("a", "b", "c"),
            new CounterfactualOptions(FixedTerms: ["a"])
        );

        Assert.Equal(["b=True c=True"], result.ChangeSets.Select(Describe));
    }

    /// <summary>When the fixed terms alone decide against the target, the result says so and holds no change set.</summary>
    [Fact]
    public void FindChanges_FixedTermsBlockTheTarget_ReportsNonePossible_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            AllFalse("a", "b"),
            new CounterfactualOptions(FixedTerms: ["a"])
        );

        Assert.Equal(CounterfactualOutcome.NonePossible, result.Outcome);
        Assert.False(result.TryGetChangeSets(out EquatableArray<ChangeSet> sets));
        Assert.Empty(sets);
    }

    /// <summary>Facts that already give the target need no change.</summary>
    [Fact]
    public void FindChanges_FactsAlreadyGiveTheTarget_ReportsNoChangeNeeded_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a OR b");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            Facts(("a", TruthValue.True), ("b", TruthValue.False))
        );

        Assert.Equal(CounterfactualOutcome.NoChangeNeeded, result.Outcome);
        Assert.Empty(result.ChangeSets);
    }

    /// <summary><c>Unknown</c> is a value a change can set: <c>IsUnknown(a)</c> becomes <c>True</c> when <c>a</c> is set to <c>Unknown</c>.</summary>
    [Fact]
    public void FindChanges_RuleThatNeedsAnUnknownTerm_SetsTheTermToUnknown_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("IsUnknown(a)");

        CounterfactualResult result = RuleCounterfactual.FindChanges(rule, AllFalse("a"));

        Assert.Equal([new TermChange("a", TruthValue.False, TruthValue.Unknown)], Assert.Single(result.ChangeSets).Changes);
    }

    /// <summary>The target can be <c>False</c>: what would make a permitted decision denied.</summary>
    [Fact]
    public void FindChanges_TargetFalse_ReturnsTheChangesThatDeny_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a OR b");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            Facts(("a", TruthValue.True), ("b", TruthValue.False)),
            new CounterfactualOptions(Target: TruthValue.False)
        );

        Assert.Equal(["a=False"], result.ChangeSets.Select(Describe));
    }

    /// <summary>
    /// A term without a fact can have any value, so a change set must work for each of them: <c>b</c> has to be set to
    /// <c>True</c>, and its <see cref="TermChange.From"/> is <see langword="null"/>.
    /// </summary>
    [Fact]
    public void FindChanges_TermWithoutAFact_IsSetWhenTheTargetDependsOnIt_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        CounterfactualResult result = RuleCounterfactual.FindChanges(rule, AllFalse("a"));

        Assert.Equal(
            [new TermChange("a", TruthValue.False, TruthValue.True), new TermChange("b", null, TruthValue.True)],
            Assert.Single(result.ChangeSets).Changes
        );
    }

    /// <summary>The number of change sets is capped, and a capped list is marked incomplete.</summary>
    [Fact]
    public void FindChanges_MoreChangeSetsThanTheCap_ReturnsTheCapAndIsIncomplete_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("ANY(a, b, c, d)");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            AllFalse("a", "b", "c", "d"),
            new CounterfactualOptions(MaxChangeSets: 2)
        );

        Assert.Equal(["a=True", "b=True"], result.ChangeSets.Select(Describe));
        Assert.False(result.IsComplete);
    }

    /// <summary>
    /// A rule with 20 distinct terms, the default term cap, is searched to the end: ten pairs of terms give ten minimal
    /// change sets of two changes each, and the list is complete.
    /// </summary>
    [Fact]
    public void FindChanges_RuleAtTheDefaultTermCap_FindsEveryMinimalChangeSet_Test()
    {
        string[] names = [.. Enumerable.Range(0, 20).Select(i => $"t{i}")];
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string name in names)
        {
            builder.AddConstant(name, false);
        }

        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(builder.Build())
            .Compile(string.Join(" OR ", Enumerable.Range(0, 10).Select(i => $"(t{2 * i} AND t{(2 * i) + 1})")))
            .GetRuleOrThrow();

        CounterfactualResult result = RuleCounterfactual.FindChanges(rule, AllFalse(names));

        Assert.Equal(10, result.ChangeSets.Count);
        Assert.All(result.ChangeSets, set => Assert.Equal(2, set.Count));
        Assert.True(result.IsComplete);
    }

    /// <summary>Beyond the term cap the search does not run and says why, as <see cref="RuleEquivalence"/> does.</summary>
    [Fact]
    public void FindChanges_MoreDistinctTermsThanTheCap_ReportsUndecidedWithReason_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            AllFalse("a", "b"),
            new CounterfactualOptions(MaxAnalysisTerms: 1)
        );

        Assert.Equal(CounterfactualOutcome.Undecided, result.Outcome);
        Assert.Empty(result.ChangeSets);
        Assert.Contains("2", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Within the configured term cap, a rule shaped so the search's shared BDD construction exceeds the internal
    /// node budget (ticket 24) reports <c>Undecided</c> with the exception's message, rather than hanging or
    /// throwing.
    /// </summary>
    [Fact]
    public void FindChanges_RulePastTheNodeBudget_ReportsUndecidedWithTheExceptionMessage_Test()
    {
        const int PairCount = 17;
        string[] xs = [.. Enumerable.Range(1, PairCount).Select(i => $"x{i}")];
        string[] ys = [.. Enumerable.Range(1, PairCount).Select(i => $"y{i}")];
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string name in xs.Concat(ys))
        {
            builder.AddConstant(name, true);
        }

        // The bad interleaved order ("every x before every y") fixes the variable order before the pairwise AND-OR
        // is built under it, forcing the exponential-under-that-order shape (the same two shapes BddNodeBudgetTests
        // uses for the comparison tools, combined into one rule here since the search takes one tree, not two).
        string badOrder = string.Join(" OR ", xs.Concat(ys));
        string pairs = string.Join(" OR ", xs.Zip(ys, (x, y) => $"({x} AND {y})"));
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(builder.Build())
            .Compile($"({badOrder}) AND ({pairs})")
            .GetRuleOrThrow();

        CounterfactualResult result = RuleCounterfactual.FindChanges(
            rule,
            new Dictionary<string, TruthValue>(),
            new CounterfactualOptions(MaxAnalysisTerms: 2 * PairCount)
        );

        Assert.Equal(CounterfactualOutcome.Undecided, result.Outcome);
        Assert.Empty(result.ChangeSets);
        Assert.Contains("budget", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>A fact keyed by a name that is not a term of the rule is refused.</summary>
    [Fact]
    public void FindChanges_FactThatIsNotATerm_ThrowsArgumentException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        Assert.Throws<ArgumentException>(() => RuleCounterfactual.FindChanges(rule, AllFalse("a", "z")));
    }

    /// <summary>A fixed term that is not a term of the rule is refused, so a misspelled fixed term cannot let the real one change.</summary>
    [Fact]
    public void FindChanges_FixedTermThatIsNotATerm_ThrowsArgumentException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        Assert.Throws<ArgumentException>(() =>
            RuleCounterfactual.FindChanges(rule, AllFalse("a"), new CounterfactualOptions(FixedTerms: ["A"]))
        );
    }

    /// <summary>A change set cap below 1 is a programming error.</summary>
    [Fact]
    public void FindChanges_ChangeSetCapBelowOne_ThrowsArgumentOutOfRangeException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RuleCounterfactual.FindChanges(rule, AllFalse("a"), new CounterfactualOptions(MaxChangeSets: 0))
        );
    }

    /// <summary>A target that is not a defined <see cref="TruthValue"/> is a programming error.</summary>
    [Fact]
    public void FindChanges_UndefinedTarget_ThrowsArgumentOutOfRangeException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RuleCounterfactual.FindChanges(rule, AllFalse("a"), new CounterfactualOptions(Target: (TruthValue)7))
        );
    }

    /// <summary>
    /// A decision of a rule with the same shape but a different term argument is refused, so its term values are not
    /// read as facts of this rule.
    /// </summary>
    [Fact]
    public async Task FindChanges_DecisionOfARuleWithADifferentTermArgument_ThrowsArgumentException_Test()
    {
        PredicateRegistryBuilder<string> builder = PredicateRegistry<string>.CreateBuilder();
        builder.Add(
            new PredicateSchema(
                "hasRole",
                "hasRole",
                "Always false.",
                [new PredicateArgumentSchema("role", "The role.", LiteralKind.String)]
            ),
            (_, _, _) => ValueTask.FromResult(TruthValue.False)
        );
        RuleCompiler<string> compiler = new(builder.Build());
        CompiledRule<string> rule = compiler.Compile("hasRole(role: \"admin\")").GetRuleOrThrow();
        Decision other = await compiler
            .Compile("hasRole(role: \"user\")")
            .GetRuleOrThrow()
            .EvaluateAsync(string.Empty, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Throws<ArgumentException>(() => RuleCounterfactual.FindChanges(rule, other));
    }

    /// <summary>
    /// For a short-circuited denial of <c>a AND b</c>, <c>b</c> was not evaluated, so the fix sets both terms and marks
    /// <c>b</c> as having no recorded value.
    /// </summary>
    [Fact]
    public async Task FindChanges_ShortCircuitedDecision_TreatsTheSkippedTermAsHavingNoFact_Test()
    {
        CompiledRule<string> rule = CompileReading("a AND b");
        Decision decision = await rule.EvaluateAsync("a=F,b=T", cancellationToken: TestContext.Current.CancellationToken);

        CounterfactualResult result = RuleCounterfactual.FindChanges(rule, decision);

        Assert.Equal(
            [new TermChange("a", TruthValue.False, TruthValue.True), new TermChange("b", null, TruthValue.True)],
            Assert.Single(result.ChangeSets).Changes
        );
    }

    /// <summary>An exhaustive decision records every term, so only the term that denied is named.</summary>
    [Fact]
    public async Task FindChanges_ExhaustiveDecision_UsesEveryRecordedTermValue_Test()
    {
        CompiledRule<string> rule = CompileReading("a AND b");
        Decision decision = await rule.EvaluateAsync(
            "a=F,b=T",
            options: new EvaluationOptions(Mode: EvaluationMode.Exhaustive),
            cancellationToken: TestContext.Current.CancellationToken
        );

        CounterfactualResult result = RuleCounterfactual.FindChanges(rule, decision);

        Assert.Equal(["a=True"], result.ChangeSets.Select(Describe));
    }

    /// <summary>A decision of a different rule, or one without a trace tree, is refused.</summary>
    /// <param name="withTrace">Whether the decision has a trace tree (of another rule).</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FindChanges_DecisionThatDoesNotMatchTheRule_ThrowsArgumentException_Test(bool withTrace)
    {
        CompiledRule<string> rule = CompileReading("a AND b");
        Decision other = await CompileReading("a OR b")
            .EvaluateAsync("a=F,b=T", cancellationToken: TestContext.Current.CancellationToken);
        Decision decision = withTrace ? other : other with { TraceTree = null };

        Assert.Throws<ArgumentException>(() => RuleCounterfactual.FindChanges(rule, decision));
    }

    /// <summary>
    /// Over many generated rules, facts (with some terms missing), fixed terms and targets, the result agrees with a
    /// brute-force search over the independent <see cref="K3Oracle"/>: the outcome is right, every change set gives the
    /// target for every value of the terms without a fact, no fixed term is changed, the sets come fewest changes first,
    /// and they are exactly the minimal change sets.
    /// </summary>
    [Fact]
    public void FindChanges_GeneratedRules_ChangeSetsAreExactlyTheMinimalOnesUnderK3Oracle_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261011);
        List<string> failures = [];
        Dictionary<CounterfactualOutcome, int> seen = [];
        TruthValue?[] factChoices = [TruthValue.True, TruthValue.False, TruthValue.Unknown, null];

        for (int i = 0; i < 400; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            CompiledRule<RuleTestContext>? rule = compiler.Compile(generated.Text).CompiledRule;
            if (rule is null)
            {
                // Out-of-range thresholds (TRE0008) are authoring errors, not search input.
                continue;
            }

            HashSet<string> ruleTerms =
            [
                .. TermNames.Where(t => Regex.IsMatch(generated.Text, $@"\b{t}\b", RegexOptions.None, TimeSpan.FromSeconds(1))),
            ];
            TruthValue?[] facts = [.. TermNames.Select(_ => factChoices[random.Next(factChoices.Length)])];
            bool[] isFixed = [.. TermNames.Select(t => ruleTerms.Contains(t) && random.Next(4) == 0)];
            TruthValue target = K3Oracle.Values[random.Next(3)];

            CounterfactualResult result = RuleCounterfactual.FindChanges(
                rule,
                TermNames
                    .Select((t, k) => (t, facts[k]))
                    .Where(p => p.Item2 is not null && ruleTerms.Contains(p.t))
                    .ToDictionary(p => p.t, p => p.Item2!.Value),
                new CounterfactualOptions(target, [.. TermNames.Where((_, k) => isFixed[k])], MaxChangeSets: 1000)
            );
            seen[result.Outcome] = seen.GetValueOrDefault(result.Outcome) + 1;

            // Terms that are not in the rule cannot matter; they are given no fact so the oracle quantifies them away.
            TruthValue?[] effective = [.. facts.Select((f, k) => ruleTerms.Contains(TermNames[k]) ? f : null)];
            List<string> minimal = OracleMinimalSets(generated, effective, isFixed, target);
            string label = $"{generated.Text} facts [{string.Join(", ", effective)}] target {target}";
            CounterfactualOutcome expected = minimal switch
            {
                [] => CounterfactualOutcome.NonePossible,
                [""] => CounterfactualOutcome.NoChangeNeeded,
                _ => CounterfactualOutcome.Found,
            };

            if (result.Outcome != expected)
            {
                failures.Add($"{label}: oracle {expected}, got {result.Outcome}");
                continue;
            }

            if (expected != CounterfactualOutcome.Found)
            {
                continue;
            }

            int[] sizes = [.. result.ChangeSets.Select(s => s.Count)];
            if (!result.IsComplete || !sizes.SequenceEqual(sizes.Order()))
            {
                failures.Add($"{label}: incomplete or unranked [{string.Join(", ", sizes)}]");
            }

            foreach (ChangeSet set in result.ChangeSets)
            {
                Dictionary<string, TruthValue> changes = set.Changes.ToDictionary(c => c.Term, c => c.To);
                if (set.Changes.Any(c => isFixed[Array.IndexOf(TermNames, c.Term)]))
                {
                    failures.Add($"{label}: {Describe(set)} changes a fixed term");
                }

                if (!Works(generated, effective, changes, target))
                {
                    failures.Add($"{label}: {Describe(set)} does not give the target");
                }
            }

            string[] returned = [.. result.ChangeSets.Select(Describe).Order(StringComparer.Ordinal)];
            if (!returned.SequenceEqual(minimal.Order(StringComparer.Ordinal)))
            {
                failures.Add($"{label}: got [{string.Join(" | ", returned)}], oracle [{string.Join(" | ", minimal)}]");
            }
        }

        Assert.Empty(failures);

        // Guard against a vacuous pass: every searched outcome must occur.
        foreach (
            CounterfactualOutcome outcome in Enum.GetValues<CounterfactualOutcome>()
                .Where(o => o != CounterfactualOutcome.Undecided)
        )
        {
            Assert.True(
                seen.GetValueOrDefault(outcome) > 10,
                $"only {seen.GetValueOrDefault(outcome)} {outcome} results were checked"
            );
        }
    }

    /// <summary>
    /// The minimal change sets by brute force: every choice of "keep" or another value for each term that is not fixed,
    /// kept when it gives the target and no smaller choice inside it does.
    /// </summary>
    private static List<string> OracleMinimalSets(GeneratedRule rule, TruthValue?[] facts, bool[] isFixed, TruthValue target)
    {
        List<Dictionary<string, TruthValue>> candidates =
        [
            [],
        ];
        for (int k = 0; k < TermNames.Length; k++)
        {
            if (isFixed[k])
            {
                continue;
            }

            int term = k;
            candidates =
            [
                .. candidates.SelectMany(c =>
                    K3Oracle
                        .Values.Where(v => v != facts[term])
                        .Select(v => new Dictionary<string, TruthValue>(c) { [TermNames[term]] = v })
                        .Prepend(c)
                ),
            ];
        }

        List<Dictionary<string, TruthValue>> working = [.. candidates.Where(c => Works(rule, facts, c, target))];
        return
        [
            .. working
                .Where(c =>
                    !working.Any(other =>
                        other.Count < c.Count && other.All(p => c.TryGetValue(p.Key, out TruthValue v) && v == p.Value)
                    )
                )
                .Select(c => string.Join(' ', c.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"))),
        ];
    }

    /// <summary>Whether the changes give the target for every value of each term that has no fact and is not changed.</summary>
    private static bool Works(
        GeneratedRule rule,
        TruthValue?[] facts,
        IReadOnlyDictionary<string, TruthValue> changes,
        TruthValue target
    )
    {
        return K3Oracle
            .Assignments(TermNames.Length)
            .Where(row =>
                TermNames.Select((t, k) => changes.TryGetValue(t, out TruthValue v) ? v : facts[k] ?? row[k]).SequenceEqual(row)
            )
            .All(row => rule.Eval(row) == target);
    }

    private static string Describe(ChangeSet set)
    {
        return string.Join(' ', set.Changes.OrderBy(c => c.Term, StringComparer.Ordinal).Select(c => $"{c.Term}={c.To}"));
    }

    private static Dictionary<string, TruthValue> Facts(params (string Term, TruthValue Value)[] facts)
    {
        return facts.ToDictionary(f => f.Term, f => f.Value);
    }

    private static Dictionary<string, TruthValue> AllFalse(params string[] terms)
    {
        return terms.ToDictionary(t => t, _ => TruthValue.False);
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        return CreateCompiler().Compile(text).GetRuleOrThrow();
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .AddConstant("d", true)
                .Build(),
            new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096)
        );
    }

    /// <summary>Compiles a rule whose terms read their value from a context string such as <c>"a=T,b=F"</c>.</summary>
    private static CompiledRule<string> CompileReading(string text)
    {
        PredicateRegistryBuilder<string> builder = PredicateRegistry<string>.CreateBuilder();
        foreach (string name in TermNames)
        {
            builder.Add(
                PredicateSchema.NoArguments(name, name, $"Reads '{name}' from the context text."),
                (context, _, _) => ValueTask.FromResult(Read(context, name))
            );
        }

        return new RuleCompiler<string>(builder.Build()).Compile(text).GetRuleOrThrow();
    }

    private static TruthValue Read(string context, string name)
    {
        foreach (string pair in context.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=');
            if (parts[0] == name)
            {
                return parts[1] switch
                {
                    "T" => TruthValue.True,
                    "F" => TruthValue.False,
                    _ => TruthValue.Unknown,
                };
            }
        }

        return TruthValue.Unknown;
    }
}
