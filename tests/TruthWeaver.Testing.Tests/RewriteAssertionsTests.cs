namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

public sealed class RewriteAssertionsTests
{
    /// <summary>Simplify keeps the meaning, never grows a rule and is idempotent, so every expectation passes.</summary>
    [Fact]
    public void AssertSound_SimplifyWithAllExpectations_DoesNotThrow_Test()
    {
        RewriteAssertions.AssertSound(Compile("(a AND TRUE) OR (a AND b)"), r => r.Simplify(), RewriteExpectations.All);
    }

    /// <summary>Canonicalize is sound, never larger and idempotent.</summary>
    [Fact]
    public void AssertSound_CanonicalizeWithAllExpectations_DoesNotThrow_Test()
    {
        RewriteAssertions.AssertSound(Compile("b AND a AND a"), r => r.Canonicalize(), RewriteExpectations.All);
    }

    /// <summary>A rewrite that changes the meaning fails the equivalence check and the message shows a counter-example.</summary>
    [Fact]
    public void AssertSound_RewriteChangesMeaning_ThrowsNamingEquivalenceWithCounterExample_Test()
    {
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RewriteAssertions.AssertSound(Compile("a AND b"), _ => Compile("a OR b"))
        );

        Assert.Contains("'equivalence'", exception.Message);
        Assert.Matches("(True|False|Unknown)", exception.Message);
    }

    /// <summary>An undecided comparison throws as inconclusive, as AssertEquivalent does.</summary>
    [Fact]
    public void AssertSound_TermCapExceeded_ThrowsInconclusive_Test()
    {
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RewriteAssertions.AssertSound(Compile("a AND b"), r => r.Canonicalize(), RewriteExpectations.None, 1)
        );

        Assert.Contains("inconclusive", exception.Message);
    }

    /// <summary>A sound rewrite that grows the rule passes by default and fails when NeverLarger is expected, with both sizes.</summary>
    [Fact]
    public void AssertSound_GrowingRewrite_FailsNeverLargerShowingSizes_Test()
    {
        CompiledRule<object?> rule = Compile("a AND b");
        static CompiledRule<object?> Grow(CompiledRule<object?> input)
        {
            return Compile($"({input.CanonicalText}) AND ({input.CanonicalText})");
        }

        RewriteAssertions.AssertSound(rule, Grow);
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RewriteAssertions.AssertSound(rule, Grow, RewriteExpectations.NeverLarger)
        );

        Assert.Contains("'never larger'", exception.Message);
        Assert.Contains($"{rule.Metrics.NodeCount} nodes", exception.Message);
        Assert.Contains($"{Grow(rule).Metrics.NodeCount} nodes", exception.Message);
    }

    /// <summary>A sound rewrite that needs two passes fails the idempotence check and shows both results.</summary>
    [Fact]
    public void AssertSound_RewriteNeedingTwoPasses_FailsIdempotent_Test()
    {
        int calls = 0;
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RewriteAssertions.AssertSound(
                Compile("a AND b"),
                _ => Compile(calls++ == 0 ? "b AND a" : "a AND b"),
                RewriteExpectations.Idempotent
            )
        );

        Assert.Contains("'idempotent'", exception.Message);
    }

    /// <summary>A null rule or rewrite is a programming error and throws ArgumentNullException.</summary>
    [Fact]
    public void AssertSound_NullArguments_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() => RewriteAssertions.AssertSound<object?>(null!, r => r));
        Assert.Throws<ArgumentNullException>(() =>
            RewriteAssertions.AssertSound(Compile("a"), (Func<CompiledRule<object?>, CompiledRule<object?>>)null!)
        );
    }

    /// <summary>The capped rewrites return a CompilationResult, and AssertSound accepts them with no CompiledRule! unwrap.</summary>
    [Fact]
    public void AssertSound_CappedRewritesReturningACompilationResult_DoNotThrow_Test()
    {
        CompiledRule<object?> rule = Compile("NOT (a AND (b OR c))");

        RewriteAssertions.AssertSound(rule, r => r.ToNnf(), RewriteExpectations.Idempotent);
        RewriteAssertions.AssertSound(rule, r => r.ToCnf(), RewriteExpectations.Idempotent);
        RewriteAssertions.AssertSound(rule, r => r.ToDnf(), RewriteExpectations.Idempotent);
        RewriteAssertions.AssertSound(rule, r => r.ExpandToNand());
    }

    /// <summary>A capped rewrite that is refused fails the assertion, and the message names the check and the diagnostic code.</summary>
    [Fact]
    public void AssertSound_CappedRewriteOverTheCap_ThrowsNamingTheRewriteDiagnostic_Test()
    {
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RewriteAssertions.AssertSound(Compile("a AND (b OR c)"), r => r.ExpandToNand(1))
        );

        Assert.Contains("'compiles'", exception.Message);
        Assert.Contains("TRE0016", exception.Message);
    }

    /// <summary>The CompilationResult overload checks the term cap and the expectations like the CompiledRule overload.</summary>
    [Fact]
    public void AssertSound_CompilationResultRewriteWithTermCapExceeded_ThrowsInconclusive_Test()
    {
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RewriteAssertions.AssertSound(Compile("a AND b"), r => r.ToDnf(), RewriteExpectations.None, 1)
        );

        Assert.Contains("inconclusive", exception.Message);
    }

    /// <summary>A null rewrite for the CompilationResult overload is a programming error and throws ArgumentNullException.</summary>
    [Fact]
    public void AssertSound_NullCompilationResultRewrite_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RewriteAssertions.AssertSound(Compile("a"), (Func<CompiledRule<object?>, CompilationResult<object?>>)null!)
        );
    }

    private static CompiledRule<object?> Compile(string text)
    {
        PredicateRegistryBuilder<object?> builder = PredicateRegistry<object?>.CreateBuilder();
        foreach (string name in new[] { "a", "b", "c" })
        {
            (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
                FakePredicates.Returning<object?>(name, true);
            builder.Add(schema, evaluate);
        }

        return new RuleCompiler<object?>(builder.Build()).Compile(text).CompiledRule!;
    }
}
