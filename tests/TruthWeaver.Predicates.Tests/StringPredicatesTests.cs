namespace TruthWeaver.Predicates.Tests;

using System.Globalization;
using TruthWeaver.Abstractions;

public class StringPredicatesTests
{
    /// <summary>Equals returns true for a match in the same case.</summary>
    [Fact]
    public async Task Equals_MatchingCase_ReturnsTrue_Test()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("equalsName", schema.Name);
        Assert.NotEmpty(schema.Label);
        Assert.NotEmpty(schema.Description);
    }

    /// <summary>Equals returns false for a match in a different case.</summary>
    [Fact]
    public async Task Equals_NonMatchingCase_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>Equals</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task Equals_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>
    /// The comparison is ordinal and does not normalize Unicode, so a precomposed character and its decomposed form
    /// are different strings.
    /// </summary>
    [Fact]
    public async Task Equals_PrecomposedAndDecomposedForms_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);
        const string precomposed = "é";
        const string decomposed = "é";

        TruthValue result = await evaluate(new TestContext(precomposed), Args("value", decomposed), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>EqualsIgnoreCase returns true for a match in a different case.</summary>
    [Fact]
    public async Task EqualsIgnoreCase_MatchingDifferentCase_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "ALICE"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>EqualsIgnoreCase returns false for a non matching string.</summary>
    [Fact]
    public async Task EqualsIgnoreCase_NonMatching_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "Bob"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>EqualsIgnoreCase</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task EqualsIgnoreCase_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>StartsWith returns true for a matching prefix.</summary>
    [Fact]
    public async Task StartsWith_MatchingPrefix_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>StartsWith returns false for a non matching prefix.</summary>
    [Fact]
    public async Task StartsWith_NonMatchingPrefix_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Bob"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>StartsWith</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task StartsWith_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>EndsWith returns true for a matching suffix.</summary>
    [Fact]
    public async Task EndsWith_MatchingSuffix_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Smith"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>EndsWith returns false for a non matching suffix.</summary>
    [Fact]
    public async Task EndsWith_NonMatchingSuffix_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Jones"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>EndsWith</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task EndsWith_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Smith"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>Contains returns true for a matching substring.</summary>
    [Fact]
    public async Task Contains_MatchingSubstring_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "ce Sm"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>Contains returns false for a non matching substring.</summary>
    [Fact]
    public async Task Contains_NonMatchingSubstring_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice Smith"), Args("value", "Bob"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>Contains</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task Contains_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>IsNullOrEmpty returns true for a null value.</summary>
    [Fact]
    public async Task IsNullOrEmpty_NullValue_ReturnsTrue_Test()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
        Assert.Empty(schema.Arguments);
    }

    /// <summary>IsNullOrEmpty returns true for an empty value.</summary>
    [Fact]
    public async Task IsNullOrEmpty_EmptyValue_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        TruthValue result = await evaluate(new TestContext(string.Empty), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>IsNullOrEmpty returns false for a non empty value.</summary>
    [Fact]
    public async Task IsNullOrEmpty_NonEmptyValue_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        TruthValue result = await evaluate(new TestContext("Alice"), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>The <c>ignoreCase</c> argument of <c>EqualsConfigurable</c> defaults to <c>false</c>, matching <c>Equals</c>.</summary>
    [Fact]
    public void EqualsConfigurable_Schema_IgnoreCaseDefaultsToFalse_Test()
    {
        (PredicateSchema schema, _) = StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        PredicateArgumentSchema ignoreCase = Assert.Single(schema.Arguments, a => a.Name == "ignoreCase");
        Assert.False(ignoreCase.Required);
        Assert.Equal(LiteralValue.OfBoolean(false), ignoreCase.Default);
    }

    /// <summary>The <c>ignoreCase</c> argument of <c>NotEqualsConfigurable</c> defaults to <c>false</c>, matching its twin.</summary>
    [Fact]
    public void NotEqualsConfigurable_Schema_IgnoreCaseDefaultsToFalse_Test()
    {
        (PredicateSchema schema, _) = StringPredicates.NotEqualsConfigurable<TestContext>(
            "notEqualsConfigurable",
            c => c.Value
        );

        PredicateArgumentSchema ignoreCase = Assert.Single(schema.Arguments, a => a.Name == "ignoreCase");
        Assert.False(ignoreCase.Required);
        Assert.Equal(LiteralValue.OfBoolean(false), ignoreCase.Default);
    }

    /// <summary>EqualsConfigurable with default shaped arguments is case insensitive under the invariant culture.</summary>
    [Fact]
    public async Task EqualsConfigurable_DefaultShapedArguments_IsCaseInsensitiveUnderInvariantCulture_Test()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("Alice"),
            ConfigurableArgs("ALICE", ignoreCase: true, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("equalsConfigurable", schema.Name);
    }

    /// <summary>EqualsConfigurable with ignore case false is case sensitive.</summary>
    [Fact]
    public async Task EqualsConfigurable_IgnoreCaseFalse_IsCaseSensitive_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("Alice"),
            ConfigurableArgs("ALICE", ignoreCase: false, trim: false),
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
            ConfigurableArgs("STRASSE", ignoreCase: true, trim: false),
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
            ConfigurableArgs("İ", ignoreCase: true, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>
    /// Verifies the schema declares no <c>culture</c> argument: comparison is always ordinal, so there is nothing
    /// for a rule author to configure (k3-followups 20).
    /// </summary>
    [Fact]
    public void EqualsConfigurable_Schema_DeclaresNoCultureArgument_Test()
    {
        (PredicateSchema schema, _) = StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        Assert.Equal(["value", "ignoreCase", "trim"], schema.Arguments.Select(a => a.Name));
    }

    /// <summary>EqualsConfigurable with trim true ignores leading and trailing whitespace.</summary>
    [Fact]
    public async Task EqualsConfigurable_TrimTrue_IgnoresLeadingAndTrailingWhitespace_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue trimmedResult = await evaluate(
            new TestContext(" Alice "),
            ConfigurableArgs("Alice", ignoreCase: false, trim: true),
            CancellationToken.None
        );
        TruthValue untrimmedResult = await evaluate(
            new TestContext(" Alice "),
            ConfigurableArgs("Alice", ignoreCase: false, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, trimmedResult);
        Assert.Equal(TruthValue.False, untrimmedResult);
    }

    /// <summary>Registered with no <c>NullBehavior</c>, <c>EqualsConfigurable</c> answers Unknown for a null selected value.</summary>
    [Fact]
    public async Task EqualsConfigurable_NullSelectedValue_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.EqualsConfigurable<TestContext>("equalsConfigurable", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext(null),
            ConfigurableArgs("Alice", ignoreCase: true, trim: false),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.Unknown, result);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }

    private static PredicateArguments ConfigurableArgs(string value, bool ignoreCase, bool trim)
    {
        return new PredicateArguments(
            new Dictionary<string, LiteralValue>
            {
                ["value"] = LiteralValue.OfString(value),
                ["ignoreCase"] = LiteralValue.OfBoolean(ignoreCase),
                ["trim"] = LiteralValue.OfBoolean(trim),
            }
        );
    }
}
