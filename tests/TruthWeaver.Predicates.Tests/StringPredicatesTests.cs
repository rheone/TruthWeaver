namespace TruthWeaver.Predicates.Tests;

using System.Globalization;
using TruthWeaver.Abstractions;

public class StringPredicatesTests
{
    [Fact]
    public async Task Equals_MatchingCase_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("equalsName", schema.Name);
        Assert.NotEmpty(schema.Label);
        Assert.NotEmpty(schema.Description);
    }

    [Fact]
    public async Task Equals_NonMatchingCase_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task Equals_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task EqualsIgnoreCase_MatchingDifferentCase_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "ALICE"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task EqualsIgnoreCase_NonMatching_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "Bob"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task EqualsIgnoreCase_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task StartsWith_MatchingPrefix_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task StartsWith_NonMatchingPrefix_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Bob"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task StartsWith_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task EndsWith_MatchingSuffix_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Smith"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task EndsWith_NonMatchingSuffix_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Jones"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task EndsWith_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Smith"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task Contains_MatchingSubstring_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "ce Sm"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task Contains_NonMatchingSubstring_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Bob"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task Contains_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task IsNullOrEmpty_NullValue_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
        Assert.Empty(schema.Arguments);
    }

    [Fact]
    public async Task IsNullOrEmpty_EmptyValue_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        TruthValue result = await evaluate(new TestContext(string.Empty), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task IsNullOrEmpty_NonEmptyValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task EqualsConfigurable_DefaultShapedArguments_IsCaseInsensitiveUnderInvariantCulture()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("Alice"),
            ConfigurableArgs("ALICE", ignoreCase: true, culture: string.Empty, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("equalsConfigurable", schema.Name);
    }

    [Fact]
    public async Task EqualsConfigurable_IgnoreCaseFalse_IsCaseSensitive()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("Alice"),
            ConfigurableArgs("ALICE", ignoreCase: false, culture: string.Empty, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>
    /// Verifies the comparison is ordinal: a linguistic comparison treats "ß" and "ss" as equal, an ordinal
    /// one does not, so the result never depends on culture tables.
    /// </summary>
    [Fact]
    public async Task EqualsConfigurable_LinguisticallyEquivalentStrings_AreNotEqualBecauseComparisonIsOrdinal_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("straße"),
            ConfigurableArgs("STRASSE", ignoreCase: true, culture: string.Empty, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>
    /// Verifies ignoreCase uses OrdinalIgnoreCase: the dotted capital I (U+0130) is not the ordinal
    /// uppercase of "i", whatever the host culture is.
    /// </summary>
    [Fact]
    public async Task EqualsConfigurable_IgnoreCaseWithDottedCapitalI_DoesNotMatchPlainI_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("i"),
            ConfigurableArgs("İ", ignoreCase: true, culture: string.Empty, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>
    /// Verifies a non-empty culture is rejected (an evaluation-time fault) rather than silently switching
    /// to culture-sensitive comparison; empty is the only accepted value.
    /// </summary>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("not!a!culture")]
    public Task EqualsConfigurable_NonEmptyCulture_ThrowsAtEvaluationTime_Test(string culture)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        return Assert.ThrowsAsync<ArgumentException>(() =>
            evaluate(
                    new TestContext("Alice"),
                    ConfigurableArgs("Alice", ignoreCase: true, culture: culture, trim: false),
                    CancellationToken.None
                )
                .AsTask()
        );
    }

    [Fact]
    public async Task EqualsConfigurable_TrimTrue_IgnoresLeadingAndTrailingWhitespace()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue trimmedResult = await evaluate(
            new TestContext(" Alice "),
            ConfigurableArgs("Alice", ignoreCase: false, culture: string.Empty, trim: true),
            CancellationToken.None
        );
        TruthValue untrimmedResult = await evaluate(
            new TestContext(" Alice "),
            ConfigurableArgs("Alice", ignoreCase: false, culture: string.Empty, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, trimmedResult);
        Assert.Equal(TruthValue.False, untrimmedResult);
    }

    [Fact]
    public async Task EqualsConfigurable_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext(null),
            ConfigurableArgs("Alice", ignoreCase: true, culture: string.Empty, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }

    private static PredicateArguments ConfigurableArgs(string value, bool ignoreCase, string culture, bool trim)
    {
        return new PredicateArguments(
            new Dictionary<string, LiteralValue>
            {
                ["value"] = LiteralValue.OfString(value),
                ["ignoreCase"] = LiteralValue.OfBoolean(ignoreCase),
                ["culture"] = LiteralValue.OfString(culture),
                ["trim"] = LiteralValue.OfBoolean(trim),
            }
        );
    }
}
