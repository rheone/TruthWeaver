namespace TruthWeaver.Tests.DocExamples;

using Microsoft.Extensions.DependencyInjection;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Evaluation;
using TruthWeaver.Predicates;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// Runs the C# snippets of <c>docs/examples.md</c> (sections 5 and 6) against the real library, because the
/// documentation checker only covers the text, JSON and YAML blocks. The worked rule has a class-based predicate,
/// a delegate predicate and a variable that a data source answers.
/// </summary>
public sealed class ExamplesGuideTests
{
    /// <summary>The rule text of the worked example in the documentation.</summary>
    internal const string WorkedRule =
        "hasTopping(topping: \"greenOlives\") AND (hasCrust(crust: \"thin\") OR hasCrust(crust: \"stuffed\", ignoreCase: true) OR (isDineIn XOR isTakeout)) AND totalAtMost(limit: from(\"limits\", \"$.maxTotal\"))";

    private const string ReviewRule =
        "(isPrimaryReviewer XOR isBackupReviewer) AND (isContractor EQUIVALENT hasSignedNda) AND AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)";

    private const string WorkedJson = """
        {
          "op": "and",
          "operands": [
            { "predicate": "hasTopping", "args": { "topping": "greenOlives" } },
            {
              "op": "or",
              "operands": [
                { "predicate": "hasCrust", "args": { "crust": "thin" } },
                { "predicate": "hasCrust", "args": { "crust": "stuffed", "ignoreCase": true } },
                { "op": "xor", "operands": [ { "predicate": "isDineIn" }, { "predicate": "isTakeout" } ] }
              ]
            },
            { "predicate": "totalAtMost", "args": { "limit": { "from": "limits", "query": "$.maxTotal" } } }
          ]
        }
        """;

    private const string WorkedYaml = """
        op: and
        operands:
          - predicate: hasTopping
            args:
              topping: "greenOlives"
          - op: or
            operands:
              - predicate: hasCrust
                args:
                  crust: "thin"
              - predicate: hasCrust
                args:
                  crust: "stuffed"
                  ignoreCase: true
              - op: xor
                operands:
                  - predicate: isDineIn
                  - predicate: isTakeout
          - predicate: totalAtMost
            args:
              limit: { from: limits, query: "$.maxTotal" }
        """;

    /// <summary>Rule text, JSON, YAML and <c>RuleBuilder</c> compile the worked rule to the same tree.</summary>
    [Fact]
    public void Compile_WorkedRuleInAllFourForms_GivesOneCanonicalText_Test()
    {
        RuleCompiler<PizzaOrder> compiler = Compiler();

        string text = compiler.Compile(WorkedRule).GetRuleOrThrow().CanonicalText;
        string json = compiler.CompileJson(WorkedJson).GetRuleOrThrow().CanonicalText;
        string yaml = compiler.CompileYaml(WorkedYaml).GetRuleOrThrow().CanonicalText;
        string built = WorkedBuilder().Compile(compiler).GetRuleOrThrow().CanonicalText;

        Assert.Equal(text, json);
        Assert.Equal(text, yaml);
        Assert.Equal(text, built);
    }

    /// <summary>The worked rule is satisfied when the data source's limit covers the order total and not when it does not.</summary>
    [Theory]
    [InlineData(40L, TruthValue.True)]
    [InlineData(10L, TruthValue.False)]
    public async Task EvaluateAsync_WorkedRuleAgainstJsonDataSource_FollowsTheLimit_Test(long maxTotal, TruthValue expected)
    {
        CompiledRule<PizzaOrder> rule = Compiler().Compile(WorkedRule).GetRuleOrThrow();
        await using ServiceProvider services = Services();
        PizzaOrder order = new("thin", new HashSet<string> { "greenOlives" }, DineIn: true, Total: 25);
        DataSources sources = new() { ["limits"] = JsonDataSource.Parse($"{{ \"maxTotal\": {maxTotal} }}") };

        Decision decision = await rule.EvaluateAsync(
            order,
            services,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A YAML document answers the same variable as the JSON document does.</summary>
    [Fact]
    public async Task EvaluateAsync_WorkedRuleAgainstYamlDataSource_MatchesTheJsonAnswer_Test()
    {
        CompiledRule<PizzaOrder> rule = Compiler().Compile(WorkedRule).GetRuleOrThrow();
        await using ServiceProvider services = Services();
        PizzaOrder order = new("thin", new HashSet<string> { "greenOlives" }, DineIn: true, Total: 25);
        DataSources sources = new() { ["limits"] = YamlDataSource.Parse("maxTotal: 40\n") };

        Decision decision = await rule.EvaluateAsync(
            order,
            services,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>Without the declared source the variable is Unknown, and the rule is not satisfied (fail-closed).</summary>
    [Fact]
    public async Task EvaluateAsync_WorkedRuleWithoutTheSource_IsUnknownWithAFault_Test()
    {
        CompiledRule<PizzaOrder> rule = Compiler().Compile(WorkedRule).GetRuleOrThrow();
        await using ServiceProvider services = Services();
        PizzaOrder order = new("thin", new HashSet<string> { "greenOlives" }, DineIn: true, Total: 25);

        Decision decision = await rule.EvaluateAsync(order, services, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Assert.Single(decision.Faults);
    }

    /// <summary>The comparison table in section 8 holds the real output of every notation for both rules.</summary>
    [Fact]
    public void Page_ComparisonTable_HoldsTheRealOutputOfEveryNotation_Test()
    {
        string page = File.ReadAllText(Path.Combine(DocExampleChecker.FindRepositoryRoot(), "docs", "examples.md"))
            .Replace("\\|", "|", StringComparison.Ordinal);
        CompiledRule<PizzaOrder> worked = Compiler().Compile(WorkedRule).GetRuleOrThrow();
        CompiledRule<PizzaOrder> review = ReviewCompiler().Compile(ReviewRule).GetRuleOrThrow();

        foreach (CompiledRule<PizzaOrder> rule in new[] { worked, review })
        {
            string word = rule.PrintRuleText();
            string unicode = rule.PrintEquation();
            string symbolic = word.Replace(" AND ", " ∧ ")
                .Replace(" OR ", " ∨ ")
                .Replace(" XOR ", " ⊕ ")
                .Replace(" EQUIVALENT ", " ↔ ");
            string cStyle = word.Replace(" AND ", " && ")
                .Replace(" OR ", " || ")
                .Replace(" XOR ", " ^ ")
                .Replace(" EQUIVALENT ", " == ");

            Assert.Equal(unicode, symbolic);
            Assert.Equal(
                rule.CanonicalText,
                (rule == worked ? Compiler() : ReviewCompiler()).Compile(symbolic).GetRuleOrThrow().CanonicalText
            );
            Assert.Contains(word, page, StringComparison.Ordinal);
            Assert.Contains(symbolic, page, StringComparison.Ordinal);
            Assert.Contains(cStyle, page, StringComparison.Ordinal);
            Assert.Contains(unicode, page, StringComparison.Ordinal);
            Assert.Contains(
                rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX }),
                page,
                StringComparison.Ordinal
            );
            Assert.Contains(
                rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.AsciiMath }),
                page,
                StringComparison.Ordinal
            );
        }
    }

    /// <summary>The C-style operator spellings in the table are the labels that the C-style tree printer uses.</summary>
    [Fact]
    public void PrintTree_CStyleOperatorLabels_AreTheSpellingsInTheTable_Test()
    {
        CompiledRule<PizzaOrder> review = ReviewCompiler().Compile(ReviewRule).GetRuleOrThrow();

        string tree = PlainTextTreePrinter.Print(review.Outline(), OperatorStyle.CStyle);

        Assert.Contains("&&", tree, StringComparison.Ordinal);
        Assert.Contains("├─ ^", tree, StringComparison.Ordinal);
        Assert.Contains("├─ ==", tree, StringComparison.Ordinal);
    }

    /// <summary>The one-line JSON in section 8 is the real output of <c>PrintJson</c>.</summary>
    [Fact]
    public void Page_PrintedJson_IsTheRealOutput_Test()
    {
        string page = File.ReadAllText(Path.Combine(DocExampleChecker.FindRepositoryRoot(), "docs", "examples.md"));

        string json = Compiler().Compile(WorkedRule).GetRuleOrThrow().PrintJson();

        Assert.Contains(json, page, StringComparison.Ordinal);
    }

    /// <summary>Builds the compiler that the documentation's registration snippet describes.</summary>
    internal static RuleCompiler<PizzaOrder> Compiler()
    {
        (
            PredicateSchema hasCrustSchema,
            Func<PizzaOrder, PredicateArguments, CancellationToken, ValueTask<TruthValue>>? hasCrustEvaluate
        ) = StringPredicates.IsEqualTo<PizzaOrder>("hasCrust", order => order.Crust, "Has Crust", argumentName: "crust");

        PredicateRegistry<PizzaOrder> registry = PredicateRegistry<PizzaOrder>
            .CreateBuilder()
            .Add<IsDineIn>()
            .Add<IsTakeout>()
            .Add(
                new PredicateSchema(
                    "hasTopping",
                    "Has Topping",
                    "Does the order include the given topping?",
                    [new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String)]
                ),
                (order, args, _) =>
                    ValueTask.FromResult(
                        order.Toppings.Contains(args.GetString("topping")) ? TruthValue.True : TruthValue.False
                    )
            )
            .Add(hasCrustSchema, hasCrustEvaluate)
            .Add(
                new PredicateSchema(
                    "totalAtMost",
                    "Total At Most",
                    "Is the order total at most the given limit?",
                    [new PredicateArgumentSchema("limit", "The highest total that is allowed.", LiteralKind.Int64)]
                ),
                (order, args, _) =>
                    ValueTask.FromResult(order.Total <= args.GetInt64("limit") ? TruthValue.True : TruthValue.False)
            )
            .Build();

        return new RuleCompiler<PizzaOrder>(
            registry,
            new CompilerOptions(DataSources: new DataSourceDeclarations { ["limits"] = JsonQueryValidator.Instance })
        );
    }

    /// <summary>Builds a compiler for the review rule of the comparison table.</summary>
    /// <returns>The compiler.</returns>
    internal static RuleCompiler<PizzaOrder> ReviewCompiler()
    {
        PredicateRegistryBuilder<PizzaOrder> builder = PredicateRegistry<PizzaOrder>.CreateBuilder();
        foreach (
            string name in new[]
            {
                "isPrimaryReviewer",
                "isBackupReviewer",
                "isContractor",
                "hasSignedNda",
                "approvedByAlice",
                "approvedByBob",
                "approvedByCarol",
            }
        )
        {
            builder.Add(
                PredicateSchema.NoArguments(name, name, "A reviewer flag."),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            );
        }

        return new RuleCompiler<PizzaOrder>(builder.Build());
    }

    /// <summary>Builds the worked rule with <c>RuleBuilder</c>, as the documentation's section 6 shows.</summary>
    /// <returns>The builder.</returns>
    internal static RuleBuilder WorkedBuilder()
    {
        return RuleBuilder.And(
            RuleBuilder.Predicate("hasTopping", ("topping", "greenOlives")),
            RuleBuilder.Or(
                RuleBuilder.Predicate("hasCrust", ("crust", "thin")),
                RuleBuilder.Predicate("hasCrust", ("crust", "stuffed"), ("ignoreCase", true)),
                RuleBuilder.Xor(RuleBuilder.Predicate("isDineIn"), RuleBuilder.Predicate("isTakeout"))
            ),
            RuleBuilder.Predicate("totalAtMost", ("limit", Arg.From("limits", "$.maxTotal")))
        );
    }

    private static ServiceProvider Services()
    {
        return new ServiceCollection().AddTransient<IsDineIn>().AddTransient<IsTakeout>().BuildServiceProvider();
    }

    /// <summary>The application context of the documentation's pizza example.</summary>
    /// <param name="Crust">The crust of the order.</param>
    /// <param name="Toppings">The toppings of the order.</param>
    /// <param name="DineIn">Whether the order is eaten in the restaurant.</param>
    /// <param name="Total">The order total.</param>
    internal sealed record PizzaOrder(string Crust, IReadOnlySet<string> Toppings, bool DineIn, long Total);

    /// <summary>A class-based predicate: the engine resolves it from the service provider on every call.</summary>
    internal sealed class IsDineIn : IPredicate<PizzaOrder>
    {
        public static PredicateSchema Schema { get; } =
            PredicateSchema.NoArguments("isDineIn", "Is Dine In", "Is the order eaten in the restaurant?");

        public ValueTask<TruthValue> EvaluateAsync(
            PizzaOrder context,
            PredicateArguments args,
            CancellationToken cancellationToken
        )
        {
            return ValueTask.FromResult(context.DineIn ? TruthValue.True : TruthValue.False);
        }
    }

    /// <summary>A class-based predicate: the engine resolves it from the service provider on every call.</summary>
    internal sealed class IsTakeout : IPredicate<PizzaOrder>
    {
        public static PredicateSchema Schema { get; } =
            PredicateSchema.NoArguments("isTakeout", "Is Takeout", "Is the order taken away?");

        public ValueTask<TruthValue> EvaluateAsync(
            PizzaOrder context,
            PredicateArguments args,
            CancellationToken cancellationToken
        )
        {
            return ValueTask.FromResult(context.DineIn ? TruthValue.False : TruthValue.True);
        }
    }
}
