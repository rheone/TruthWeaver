namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A decision made over a quantifier whose collection selector returned <see langword="null"/> is accepted by rule
/// coverage and <c>Explain</c> for the same rule, and a decision whose trace only claims a null collection is rejected.
/// </summary>
public sealed class NullCollectionQuantifierDecisionTests
{
    private const string Rule = "FORALL(x IN items, elementTrue)";

    /// <summary>Rule coverage counts a decision whose quantifier collection was null instead of rejecting its trace tree.</summary>
    [Fact]
    public async Task FromDecisions_QuantifierOverANullCollection_CountsTheDecisionAsUnknown_Test()
    {
        CompiledRule<CollectionContext> rule = Compile(Rule);
        Decision decision = await EvaluateAsync(rule, new CollectionContext());

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, [decision]);

        Assert.Equal(1, coverage.UnknownCount);
    }

    /// <summary>Rule coverage counts a decision whose nested quantifier collection was null, inside an element of the outer collection.</summary>
    [Fact]
    public async Task FromDecisions_NestedQuantifierOverANullCollection_CountsTheDecisionAsUnknown_Test()
    {
        CompiledRule<CollectionContext> rule = Compile("FORALL(g IN groups, EXISTS(m IN members, elementTrue))");
        Decision decision = await EvaluateAsync(rule, new CollectionContext { Groups = [null] });

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, [decision]);

        Assert.Equal(1, coverage.UnknownCount);
    }

    /// <summary>Explain describes a null-collection decision as Unknown instead of rejecting its trace tree.</summary>
    [Fact]
    public async Task Explain_QuantifierOverANullCollection_DescribesAnUnknownResult_Test()
    {
        CompiledRule<CollectionContext> rule = Compile(Rule);
        Decision decision = await EvaluateAsync(rule, new CollectionContext());

        string sentence = rule.Explain(decision);

        Assert.Contains("unknown", sentence, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A trace that claims a null collection but holds element evaluations is not from this rule, so it is rejected.</summary>
    [Fact]
    public async Task FromDecisions_NoCollectionTextOnAQuantifierWithElements_ThrowsAtTheQuantifier_Test()
    {
        CompiledRule<CollectionContext> rule = Compile(Rule);
        Decision decision = await EvaluateAsync(rule, new CollectionContext { Items = [TruthValue.True] });
        TraceNode root = decision.TraceTree!;
        Decision tampered = decision with { TraceTree = root with { Text = root.Text + TraceText.NoCollectionSuffix } };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A trace that claims a null collection but holds a definite result is not from this rule, so it is rejected.</summary>
    [Fact]
    public async Task FromDecisions_NoCollectionTextWithADefiniteResult_ThrowsAtTheQuantifier_Test()
    {
        CompiledRule<CollectionContext> rule = Compile(Rule);
        Decision decision = await EvaluateAsync(rule, new CollectionContext());
        Decision tampered = decision with
        {
            Result = TruthValue.True,
            TraceTree = decision.TraceTree! with { Result = TruthValue.True },
        };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A null-collection trace for a different quantifier is not from this rule, so it is rejected.</summary>
    [Fact]
    public async Task FromDecisions_NoCollectionTextOfAnotherQuantifier_ThrowsAtTheQuantifier_Test()
    {
        CompiledRule<CollectionContext> rule = Compile(Rule);
        Decision decision = await EvaluateAsync(rule, new CollectionContext());
        Decision tampered = decision with
        {
            TraceTree = decision.TraceTree! with { Text = $"EXISTS(x IN items){TraceText.NoCollectionSuffix}" },
        };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    private static Task<Decision> EvaluateAsync(CompiledRule<CollectionContext> rule, CollectionContext context)
    {
        return rule.EvaluateAsync(context, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static CompiledRule<CollectionContext> Compile(string text)
    {
        return CollectionContext.CreateCompiler().Compile(text).GetRuleOrThrow();
    }
}
