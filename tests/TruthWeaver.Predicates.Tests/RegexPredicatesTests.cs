namespace TruthWeaver.Predicates.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;

public class RegexPredicatesTests
{
    [Fact]
    public async Task Matches_MatchingPattern_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("alice@example.com"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("matchesEmail", schema.Name);
    }

    [Fact]
    public async Task Matches_NonMatchingPattern_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("not-an-email"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>Matches</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task Matches_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("pattern", @"^\S+@\S+\.\S+$"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    [Fact]
    public Task Matches_InvalidPattern_ThrowsAtEvaluationTime()
    {
        // ADR-0001's Kleene failure model: the predicate contract signals "cannot determine this" by
        // simply throwing, and the evaluator (not this package) is what turns that into Unknown. This
        // package's job here is only to confirm it does not silently swallow the invalid pattern.
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args("pattern", "("), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Matches_SamePatternReusedAcrossCalls_UsesCachedRegex()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesDigits", c => c.Value);

        TruthValue first = await evaluate(new TestContext("123"), Args("pattern", @"^\d+$"), CancellationToken.None);
        TruthValue second = await evaluate(new TestContext("456"), Args("pattern", @"^\d+$"), CancellationToken.None);

        Assert.Equal(TruthValue.True, first);
        Assert.Equal(TruthValue.True, second);
    }

    /// <summary><c>MatchesIgnoreCase</c> matches a value that differs from the pattern only in case, where <c>Matches</c> does not.</summary>
    [Fact]
    public async Task MatchesIgnoreCase_ValueDiffersOnlyInCase_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.MatchesIgnoreCase<TestContext>("m", c => c.Value);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> caseSensitive) =
            RegexPredicates.Matches<TestContext>("m", c => c.Value);

        TruthValue ignoring = await evaluate(new TestContext("ABC"), Args("pattern", "^abc$"), CancellationToken.None);
        TruthValue sensitive = await caseSensitive(new TestContext("ABC"), Args("pattern", "^abc$"), CancellationToken.None);

        Assert.Equal(TruthValue.True, ignoring);
        Assert.Equal(TruthValue.False, sensitive);
    }

    /// <summary><c>NotMatchesIgnoreCase</c> is False for a value that differs from the pattern only in case.</summary>
    [Fact]
    public async Task NotMatchesIgnoreCase_ValueDiffersOnlyInCase_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.NotMatchesIgnoreCase<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(new TestContext("ABC"), Args("pattern", "^abc$"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>An inline option in the pattern still wins over the registration option.</summary>
    [Fact]
    public async Task MatchesIgnoreCase_PatternTurnsCaseSensitivityBackOn_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.MatchesIgnoreCase<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(new TestContext("ABC"), Args("pattern", "(?-i)^abc$"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>An invalid pattern from a data source throws at evaluation, as for <c>Matches</c>.</summary>
    [Fact]
    public Task MatchesIgnoreCase_InvalidPattern_ThrowsAtEvaluationTime_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.MatchesIgnoreCase<TestContext>("m", c => c.Value);

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args("pattern", "("), CancellationToken.None)
        );
    }

    /// <summary>The validator rejects an invalid literal pattern, for each of the four factories.</summary>
    [Fact]
    public void ArgumentValidator_InvalidLiteralPattern_ReturnsOneProblem_Test()
    {
        PredicateSchema[] schemas =
        [
            RegexPredicates.Matches<TestContext>("a", c => c.Value).Schema,
            RegexPredicates.NotMatches<TestContext>("a", c => c.Value).Schema,
            RegexPredicates.MatchesIgnoreCase<TestContext>("a", c => c.Value).Schema,
            RegexPredicates.NotMatchesIgnoreCase<TestContext>("a", c => c.Value).Schema,
        ];

        Assert.All(
            schemas,
            schema =>
            {
                PredicateArgumentProblem problem = Assert.Single(schema.ArgumentValidator!(Args("pattern", "(")));
                Assert.Contains("'pattern'", problem.Message, StringComparison.Ordinal);
            }
        );
    }

    /// <summary>The validator accepts a valid pattern and skips a pattern that is absent because a data source supplies it.</summary>
    [Fact]
    public void ArgumentValidator_ValidOrAbsentPattern_ReturnsNoProblem_Test()
    {
        PredicateSchema schema = RegexPredicates.Matches<TestContext>("a", c => c.Value).Schema;

        Assert.Empty(schema.ArgumentValidator!(Args("pattern", "^a")));
        Assert.Empty(schema.ArgumentValidator(PredicateArguments.Empty));
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
