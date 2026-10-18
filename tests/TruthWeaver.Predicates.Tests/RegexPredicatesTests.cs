namespace TruthWeaver.Predicates.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;

public class RegexPredicatesTests
{
    /// <summary>IsMatch returns true for a matching pattern.</summary>
    [Fact]
    public async Task IsMatch_MatchingPattern_ReturnsTrue_Test()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("alice@example.com"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("matchesEmail", schema.Name);
    }

    /// <summary>IsMatch returns false for a non matching pattern.</summary>
    [Fact]
    public async Task IsMatch_NonMatchingPattern_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("not-an-email"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>IsMatch</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task IsMatch_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("pattern", @"^\S+@\S+\.\S+$"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>IsMatch throws at evaluation time for an invalid pattern.</summary>
    [Fact]
    public Task IsMatch_InvalidPattern_ThrowsAtEvaluationTime_Test()
    {
        // ADR-0001's Kleene failure model: the predicate contract signals "cannot determine this" by
        // simply throwing, and the evaluator (not this package) is what turns that into Unknown. This
        // package's job here is only to confirm it does not silently swallow the invalid pattern.
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("matchesEmail", c => c.Value);

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args("pattern", "("), CancellationToken.None)
        );
    }

    /// <summary>IsMatch reuses a cached regex for a pattern used across calls.</summary>
    [Fact]
    public async Task IsMatch_SamePatternReusedAcrossCalls_UsesCachedRegex_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("matchesDigits", c => c.Value);

        TruthValue first = await evaluate(new TestContext("123"), Args("pattern", @"^\d+$"), CancellationToken.None);
        TruthValue second = await evaluate(new TestContext("456"), Args("pattern", @"^\d+$"), CancellationToken.None);

        Assert.Equal(TruthValue.True, first);
        Assert.Equal(TruthValue.True, second);
    }

    /// <summary>The <c>i</c> flag matches a value that differs from the pattern only in case, where no flags do not.</summary>
    [Fact]
    public async Task IsMatch_FlagI_ValueDiffersOnlyInCase_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue ignoring = await evaluate(
            new TestContext("ABC"),
            Args(("pattern", "^abc$"), ("flags", "i")),
            CancellationToken.None
        );
        TruthValue sensitive = await evaluate(new TestContext("ABC"), Args("pattern", "^abc$"), CancellationToken.None);

        Assert.Equal(TruthValue.True, ignoring);
        Assert.Equal(TruthValue.False, sensitive);
    }

    /// <summary>An empty flags string means no options.</summary>
    [Fact]
    public async Task IsMatch_EmptyFlags_IsCaseSensitive_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("ABC"),
            Args(("pattern", "^abc$"), ("flags", string.Empty)),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary><c>IsNotMatch</c> with the <c>i</c> flag is False for a value that differs from the pattern only in case.</summary>
    [Fact]
    public async Task IsNotMatch_FlagI_ValueDiffersOnlyInCase_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsNotMatch<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("ABC"),
            Args(("pattern", "^abc$"), ("flags", "i")),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>An inline option in the pattern still wins over the <c>i</c> flag.</summary>
    [Fact]
    public async Task IsMatch_FlagIAndPatternTurnsCaseSensitivityBackOn_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("ABC"),
            Args(("pattern", "(?-i)^abc$"), ("flags", "i")),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>The <c>m</c> flag lets <c>^</c> and <c>$</c> match at line breaks.</summary>
    [Fact]
    public async Task IsMatch_FlagM_AnchorsMatchAtLineBreaks_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue multiline = await evaluate(
            new TestContext("a\nb"),
            Args(("pattern", "^b$"), ("flags", "m")),
            CancellationToken.None
        );
        TruthValue plain = await evaluate(new TestContext("a\nb"), Args("pattern", "^b$"), CancellationToken.None);

        Assert.Equal(TruthValue.True, multiline);
        Assert.Equal(TruthValue.False, plain);
    }

    /// <summary>The <c>s</c> flag lets <c>.</c> match a line break.</summary>
    [Fact]
    public async Task IsMatch_FlagS_DotMatchesLineBreak_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue singleLine = await evaluate(
            new TestContext("a\nb"),
            Args(("pattern", "a.b"), ("flags", "s")),
            CancellationToken.None
        );
        TruthValue plain = await evaluate(new TestContext("a\nb"), Args("pattern", "a.b"), CancellationToken.None);

        Assert.Equal(TruthValue.True, singleLine);
        Assert.Equal(TruthValue.False, plain);
    }

    /// <summary>The <c>n</c> flag turns unnamed groups into non-capturing groups, so a numbered back-reference no longer resolves.</summary>
    [Fact]
    public Task IsMatch_FlagN_UnnamedGroupDoesNotCapture_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        // Without n, \1 refers to the first group. With n there is no group 1, so the pattern is invalid.
        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("aa"), Args(("pattern", @"(a)\1"), ("flags", "n")), CancellationToken.None)
        );
    }

    /// <summary>The <c>x</c> flag ignores white space and comments in the pattern.</summary>
    [Fact]
    public async Task IsMatch_FlagX_IgnoresPatternWhitespace_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue extended = await evaluate(
            new TestContext("ab"),
            Args(("pattern", "a b # comment"), ("flags", "x")),
            CancellationToken.None
        );
        TruthValue plain = await evaluate(new TestContext("ab"), Args("pattern", "a b # comment"), CancellationToken.None);

        Assert.Equal(TruthValue.True, extended);
        Assert.Equal(TruthValue.False, plain);
    }

    /// <summary>Flags from a data source that are invalid throw at evaluation, so the evaluator records a fault.</summary>
    [Theory]
    [InlineData("q")]
    [InlineData("ii")]
    public Task IsMatch_InvalidFlagsAtEvaluation_Throws_Test(string flags)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        return Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new TestContext("anything"), Args(("pattern", "a"), ("flags", flags)), CancellationToken.None)
        );
    }

    /// <summary>A bad data-source pattern or flags value throws for a null selection too, so the evaluator records a fault.</summary>
    [Theory]
    [InlineData(true, "(", "")]
    [InlineData(true, "a", "q")]
    [InlineData(false, "(", "")]
    [InlineData(false, "a", "ii")]
    public Task IsMatchAndIsNotMatch_BadPatternOrFlagsWithNullSelection_Throws_Test(bool negated, string pattern, string flags)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) = negated
            ? RegexPredicates.IsNotMatch<TestContext>("m", c => c.Value)
            : RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        return Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await evaluate(new TestContext(null), Args(("pattern", pattern), ("flags", flags)), CancellationToken.None)
        );
    }

    /// <summary>For <c>IsMatch</c>, a good pattern and flags with a null selection still answer per <c>NullBehavior</c>.</summary>
    [Fact]
    public async Task IsMatch_GoodPatternWithNullSelection_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext(null),
            Args(("pattern", "a"), ("flags", "i")),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>A good pattern with a null selection still answers per <c>NullBehavior</c>.</summary>
    [Fact]
    public async Task IsNotMatch_GoodPatternWithNullSelection_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsNotMatch<TestContext>("m", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext(null),
            Args(("pattern", "a"), ("flags", "i")),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>An invalid pattern from a data source throws at evaluation when the <c>i</c> flag is set, as with no flags.</summary>
    [Fact]
    public Task IsMatch_FlagIAndInvalidPattern_ThrowsAtEvaluationTime_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.IsMatch<TestContext>("m", c => c.Value);

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args(("pattern", "("), ("flags", "i")), CancellationToken.None)
        );
    }

    /// <summary>The schema declares <c>flags</c> as an optional String argument that defaults to the empty string.</summary>
    [Fact]
    public void Schema_FlagsArgument_IsOptionalStringDefaultingToEmpty_Test()
    {
        PredicateSchema[] schemas =
        [
            RegexPredicates.IsMatch<TestContext>("a", c => c.Value).Schema,
            RegexPredicates.IsNotMatch<TestContext>("a", c => c.Value).Schema,
        ];

        Assert.All(
            schemas,
            schema =>
            {
                Assert.Equal(["pattern", "flags"], schema.Arguments.Select(a => a.Name));
                PredicateArgumentSchema flags = schema.Arguments[1];
                Assert.Equal(LiteralKind.String, flags.Type);
                Assert.False(flags.Required);
                Assert.Equal(LiteralValue.OfString(string.Empty), flags.Default);
            }
        );
    }

    /// <summary>The validator rejects an invalid literal pattern, for each of the two factories.</summary>
    [Fact]
    public void ArgumentValidator_InvalidLiteralPattern_ReturnsOneProblem_Test()
    {
        PredicateSchema[] schemas =
        [
            RegexPredicates.IsMatch<TestContext>("a", c => c.Value).Schema,
            RegexPredicates.IsNotMatch<TestContext>("a", c => c.Value).Schema,
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

    /// <summary>The validator rejects an unknown or a repeated literal flag letter with one problem that names <c>flags</c>.</summary>
    [Theory]
    [InlineData("q")]
    [InlineData("I")]
    [InlineData("ii")]
    [InlineData("imi")]
    public void ArgumentValidator_InvalidLiteralFlags_ReturnsOneProblem_Test(string flags)
    {
        PredicateSchema schema = RegexPredicates.IsMatch<TestContext>("a", c => c.Value).Schema;

        PredicateArgumentProblem problem = Assert.Single(schema.ArgumentValidator!(Args(("pattern", "a"), ("flags", flags))));

        Assert.Contains("'flags'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>The validator checks the pattern under the flags: a pattern that is only valid with <c>x</c> passes with it.</summary>
    [Fact]
    public void ArgumentValidator_PatternValidOnlyUnderFlags_ReturnsNoProblem_Test()
    {
        PredicateSchema schema = RegexPredicates.IsMatch<TestContext>("a", c => c.Value).Schema;

        Assert.Empty(schema.ArgumentValidator!(Args(("pattern", "a b"), ("flags", "imsnx"))));
    }

    /// <summary>The validator accepts a valid pattern and skips a pattern that is absent because a data source supplies it.</summary>
    [Fact]
    public void ArgumentValidator_ValidOrAbsentPattern_ReturnsNoProblem_Test()
    {
        PredicateSchema schema = RegexPredicates.IsMatch<TestContext>("a", c => c.Value).Schema;

        Assert.Empty(schema.ArgumentValidator!(Args("pattern", "^a")));
        Assert.Empty(schema.ArgumentValidator(PredicateArguments.Empty));
    }

    private static PredicateArguments Args(string name, string value)
    {
        return Args((name, value));
    }

    private static PredicateArguments Args(params (string Name, string Value)[] arguments)
    {
        return new PredicateArguments(arguments.ToDictionary(a => a.Name, a => LiteralValue.OfString(a.Value)));
    }
}
