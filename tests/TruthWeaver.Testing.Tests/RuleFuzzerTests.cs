namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

public sealed class RuleFuzzerTests
{
    /// <summary><see cref="RuleFuzzerOptions.Default"/> equals a new instance and holds the documented sizes.</summary>
    [Fact]
    public void Default_ComparedWithANewInstance_IsEqualAndHoldsTheDocumentedSizes_Test()
    {
        Assert.Equal(new RuleFuzzerOptions(), RuleFuzzerOptions.Default);
        Assert.Equal(200, RuleFuzzerOptions.Default.RuleCount);
        Assert.Equal(3, RuleFuzzerOptions.Default.MaxDepth);
        Assert.Equal(3, RuleFuzzerOptions.Default.MaxTerms);
    }

    /// <summary>A run over a registry of correct predicates checks many rules and finds no failure.</summary>
    [Fact]
    public async Task RunAsync_RegistryOfZeroArgumentPredicates_PassesEveryCheck_Test()
    {
        PredicateRegistry<object?> registry = PredicateRegistry<object?>
            .CreateBuilder()
            .Add(PredicateSchema.NoArguments("isActive", "Is active", "Is the account active?"), TrueAsync)
            .Add(PredicateSchema.NoArguments("isAdmin", "Is admin", "Is the user an administrator?"), TrueAsync)
            .Add(PredicateSchema.NoArguments("isLocked", "Is locked", "Is the account locked?"), TrueAsync)
            .Build();

        RuleFuzzReport report = await RuleFuzzer.RunAsync(
            registry,
            seed: 42,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(report.Passed, report.ToString());
        Assert.True(report.RulesChecked > 100, $"only {report.RulesChecked} rules were checked");
    }

    /// <summary>
    /// The fuzzer reads only the schemas: predicates with arguments, a predicate that always throws and a class-based
    /// predicate that needs a service are all fuzzed, and their calls carry generated arguments.
    /// </summary>
    [Fact]
    public async Task RunAsync_PredicatesThatCannotRun_FuzzesTheirSchemas_Test()
    {
        PredicateSchema hasRole = new(
            "hasRole",
            "Has role",
            "Does the user hold the role?",
            [
                new PredicateArgumentSchema("role", "The role code.", LiteralKind.String),
                new PredicateArgumentSchema(
                    "scope",
                    "The scope.",
                    LiteralKind.String,
                    Required: false,
                    LiteralValue.OfString("all")
                ),
            ]
        );
        PredicateRegistry<object?> registry = PredicateRegistry<object?>
            .CreateBuilder()
            .Add(hasRole, (_, _, _) => throw new InvalidOperationException("No user store in this test."))
            .Add<NeedsServicePredicate>()
            .Build();

        RuleFuzzReport report = await RuleFuzzer.RunAsync(
            registry,
            seed: 7,
            new RuleFuzzerOptions { RuleCount = 50 },
            TestContext.Current.CancellationToken
        );

        Assert.True(report.Passed, report.ToString());
        Assert.Equal(["hasRole(role: \"text\", scope: \"all\")", "needsService"], report.Terms);
        Assert.Empty(report.SkippedPredicates);
    }

    /// <summary>A predicate whose call does not compile with the generated arguments is skipped and named in the report.</summary>
    [Fact]
    public async Task RunAsync_PredicateRejectsGeneratedArguments_ListsItAsSkipped_Test()
    {
        PredicateSchema strict = new(
            "strict",
            "Strict",
            "Rejects every argument value.",
            [new PredicateArgumentSchema("code", "A code.", LiteralKind.Int64)]
        )
        {
            ArgumentValidator = _ => [new PredicateArgumentProblem("The code is never valid.", "a valid code", "1")],
        };
        PredicateRegistry<object?> registry = PredicateRegistry<object?>
            .CreateBuilder()
            .Add(strict, TrueAsync)
            .Add(PredicateSchema.NoArguments("isActive", "Is active", "Is the account active?"), TrueAsync)
            .Build();

        RuleFuzzReport report = await RuleFuzzer.RunAsync(
            registry,
            seed: 1,
            new RuleFuzzerOptions { RuleCount = 10 },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(["strict"], report.SkippedPredicates);
        Assert.Equal(["isActive"], report.Terms);
    }

    /// <summary>
    /// A rewrite that changes the meaning of a rule fails the matching check, and the failure names the seed, the rule
    /// index and the rule text.
    /// </summary>
    [Fact]
    public async Task RunAsync_RewriteChangesMeaning_ReportsSeedAndRuleText_Test()
    {
        RuleFuzzReport report = await RunWithNegatingSimplifyAsync(seed: 99);

        Assert.False(report.Passed);
        RuleFuzzFailure failure = report.Failures[0];
        Assert.Equal(RuleFuzzCheck.Simplify, failure.Check);
        Assert.Equal(99, failure.Seed);
        Assert.Contains("seed 99", failure.ToString());
        Assert.Contains(failure.RuleText, failure.ToString());
        Assert.Contains("Strong Kleene gives", failure.Detail);
    }

    /// <summary>A rewrite that grows the rule fails the SimplifyNeverLarger check and the detail shows both sizes.</summary>
    [Fact]
    public async Task RunAsync_RewriteGrowsTheRule_ReportsSimplifyNeverLarger_Test()
    {
        RuleFuzzReport report = await RunWithNegatingSimplifyAsync(seed: 99);

        RuleFuzzFailure failure = report.Failures.First(f => f.Check == RuleFuzzCheck.SimplifyNeverLarger);
        Assert.Contains("nodes", failure.Detail);
        Assert.DoesNotContain(report.Failures, f => f.Check == RuleFuzzCheck.CanonicalizeNeverLarger);
    }

    /// <summary>The same seed gives the same rules, so a second run reproduces each failure exactly.</summary>
    [Fact]
    public async Task RunAsync_SameSeed_ReproducesTheFailures_Test()
    {
        RuleFuzzReport first = await RunWithNegatingSimplifyAsync(seed: 2026);
        RuleFuzzReport second = await RunWithNegatingSimplifyAsync(seed: 2026);

        Assert.NotEmpty(first.Failures);
        Assert.Equal(first.Failures, second.Failures);
    }

    /// <summary>ShouldPass throws a RuleFuzzException whose message has the seed and each failed rule.</summary>
    [Fact]
    public async Task ShouldPass_FailedRun_ThrowsWithSeedAndRuleText_Test()
    {
        RuleFuzzReport report = await RunWithNegatingSimplifyAsync(seed: 5);

        RuleFuzzException exception = Assert.Throws<RuleFuzzException>(report.ShouldPass);

        Assert.Contains("seed 5", exception.Message);
        Assert.Contains(report.Failures[0].RuleText, exception.Message);
    }

    /// <summary>A rule count of zero is a usage error.</summary>
    [Fact]
    public async Task RunAsync_ZeroRuleCount_ThrowsArgumentOutOfRangeException_Test()
    {
        PredicateRegistry<object?> registry = PredicateRegistry<object?>
            .CreateBuilder()
            .Add(PredicateSchema.NoArguments("isActive", "Is active", "Is the account active?"), TrueAsync)
            .Build();

        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            RuleFuzzer.RunAsync(
                registry,
                seed: 1,
                new RuleFuzzerOptions { RuleCount = 0 },
                TestContext.Current.CancellationToken
            )
        );

        Assert.Contains("RuleCount", exception.Message);
    }

    /// <summary>A registry with no predicate that the fuzzer can call is a usage error.</summary>
    [Fact]
    public async Task RunAsync_EmptyRegistry_ThrowsArgumentException_Test()
    {
        PredicateRegistry<object?> registry = PredicateRegistry<object?>.CreateBuilder().Build();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            RuleFuzzer.RunAsync(registry, seed: 1, cancellationToken: TestContext.Current.CancellationToken)
        );

        Assert.Contains("no predicate", exception.Message);
    }

    /// <summary>Runs the fuzzer with a broken Simplify step that wraps each rule in NOT, which changes every definite value.</summary>
    private static Task<RuleFuzzReport> RunWithNegatingSimplifyAsync(int seed)
    {
        PredicateSchema[] schemas =
        [
            PredicateSchema.NoArguments("a", "A", "Input a."),
            PredicateSchema.NoArguments("b", "B", "Input b."),
        ];
        return RuleFuzzer.RunAsync(
            schemas,
            seed,
            new RuleFuzzerOptions { RuleCount = 20 },
            (rule, compiler) => compiler.Compile($"NOT ({rule.CanonicalText})").CompiledRule!,
            TestContext.Current.CancellationToken
        );
    }

    private static ValueTask<TruthValue> TrueAsync(
        object? context,
        PredicateArguments arguments,
        CancellationToken cancellationToken
    )
    {
        return ValueTask.FromResult(TruthValue.True);
    }

    /// <summary>A class-based predicate that only a service provider can create, so it cannot run in this test.</summary>
    private sealed class NeedsServicePredicate(IServiceProvider services) : IPredicate<object?>
    {
        private readonly IServiceProvider services = services;

        public static PredicateSchema Schema { get; } =
            PredicateSchema.NoArguments("needsService", "Needs service", "Asks a service for the answer.");

        public ValueTask<TruthValue> EvaluateAsync(
            object? context,
            PredicateArguments args,
            CancellationToken cancellationToken
        )
        {
            return ValueTask.FromResult(this.services.GetService(typeof(object)) is null ? TruthValue.False : TruthValue.True);
        }
    }
}
