namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

/// <summary>
/// The <c>NotX</c> twins of <c>EqualsIgnoreCase</c>, <c>StartsWith</c>, <c>EndsWith</c>, <c>EqualsConfigurable</c> and
/// <c>SetEquals</c> answer the complement of their positive predicate for a non-null selected value.
/// </summary>
public sealed class AddedTwinPredicatesTests
{
    private delegate ValueTask<TruthValue> Evaluate(TestContext context, PredicateArguments args, CancellationToken token);

    /// <summary><c>NotEqualsIgnoreCase</c> is False for a value equal to the argument in another case, and True otherwise.</summary>
    /// <param name="selected">The selected string.</param>
    /// <param name="expected">The expected twin answer.</param>
    [Theory]
    [InlineData("ALICE", TruthValue.False)]
    [InlineData("bob", TruthValue.True)]
    public async Task NotEqualsIgnoreCase_NonNullSelection_AnswersComplementOfEqualsIgnoreCase_Test(
        string selected,
        TruthValue expected
    )
    {
        Evaluate evaluate = new(StringPredicates.NotEqualsIgnoreCase<TestContext>("p", c => c.Value).Evaluate);

        TruthValue result = await evaluate(new TestContext(selected), Args("value", "alice"), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    /// <summary><c>NotStartsWith</c> is False for a value with the prefix, and True for one without it.</summary>
    /// <param name="selected">The selected string.</param>
    /// <param name="expected">The expected twin answer.</param>
    [Theory]
    [InlineData("prefix-rest", TruthValue.False)]
    [InlineData("rest-prefix", TruthValue.True)]
    [InlineData("PREFIX-rest", TruthValue.True)]
    public async Task NotStartsWith_NonNullSelection_AnswersComplementOfStartsWith_Test(string selected, TruthValue expected)
    {
        Evaluate evaluate = new(StringPredicates.NotStartsWith<TestContext>("p", c => c.Value).Evaluate);

        TruthValue result = await evaluate(new TestContext(selected), Args("value", "prefix"), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    /// <summary><c>NotEndsWith</c> is False for a value with the suffix, and True for one without it.</summary>
    /// <param name="selected">The selected string.</param>
    /// <param name="expected">The expected twin answer.</param>
    [Theory]
    [InlineData("rest.txt", TruthValue.False)]
    [InlineData("txt.rest", TruthValue.True)]
    [InlineData("rest.TXT", TruthValue.True)]
    public async Task NotEndsWith_NonNullSelection_AnswersComplementOfEndsWith_Test(string selected, TruthValue expected)
    {
        Evaluate evaluate = new(StringPredicates.NotEndsWith<TestContext>("p", c => c.Value).Evaluate);

        TruthValue result = await evaluate(new TestContext(selected), Args("value", ".txt"), CancellationToken.None);

        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <c>NotEqualsConfigurable</c> applies the <c>ignoreCase</c> and <c>trim</c> arguments as <c>EqualsConfigurable</c>
    /// does and answers the complement.
    /// </summary>
    /// <param name="selected">The selected string.</param>
    /// <param name="ignoreCase">The <c>ignoreCase</c> argument.</param>
    /// <param name="trim">The <c>trim</c> argument.</param>
    /// <param name="expected">The expected twin answer.</param>
    [Theory]
    [InlineData("Alice", true, false, TruthValue.False)]
    [InlineData("Alice", false, false, TruthValue.True)]
    [InlineData(" alice ", false, true, TruthValue.False)]
    [InlineData(" alice ", false, false, TruthValue.True)]
    public async Task NotEqualsConfigurable_NonNullSelection_AnswersComplementOfEqualsConfigurable_Test(
        string selected,
        bool ignoreCase,
        bool trim,
        TruthValue expected
    )
    {
        Evaluate evaluate = new(StringPredicates.NotEqualsConfigurable<TestContext>("p", c => c.Value).Evaluate);
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["value"] = LiteralValue.OfString("alice"),
                ["ignoreCase"] = LiteralValue.OfBoolean(ignoreCase),
                ["trim"] = LiteralValue.OfBoolean(trim),
            }
        );

        TruthValue result = await evaluate(new TestContext(selected), args, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    /// <summary><c>NotEqualsConfigurable</c> declares the same arguments as <c>EqualsConfigurable</c>, with the same defaults.</summary>
    [Fact]
    public void NotEqualsConfigurable_Schema_DeclaresTheArgumentsOfEqualsConfigurable_Test()
    {
        PredicateSchema positive = StringPredicates.EqualsConfigurable<TestContext>("p", c => c.Value).Schema;

        PredicateSchema twin = StringPredicates.NotEqualsConfigurable<TestContext>("p", c => c.Value).Schema;

        Assert.Equal(
            positive.Arguments.Select(argument => (argument.Name, argument.Type, argument.Required, argument.Default)),
            twin.Arguments.Select(argument => (argument.Name, argument.Type, argument.Required, argument.Default))
        );
    }

    /// <summary><c>NotSetEquals</c> is False for the same distinct elements in any order, and True for different ones.</summary>
    /// <param name="selected">The selected collection, comma-separated.</param>
    /// <param name="expected">The expected twin answer.</param>
    [Theory]
    [InlineData("b,a,a", TruthValue.False)]
    [InlineData("a", TruthValue.True)]
    [InlineData("a,b,c", TruthValue.True)]
    public async Task NotSetEquals_NonNullSelection_AnswersComplementOfSetEquals_Test(string selected, TruthValue expected)
    {
        Evaluate evaluate = new(CollectionPredicates.NotSetEquals<TestContext>("p", c => c.Values).Evaluate);
        LiteralValue values = LiteralValue.OfArray(
            LiteralKind.String,
            [LiteralValue.OfString("a"), LiteralValue.OfString("b")]
        );
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["values"] = values });

        TruthValue result = await evaluate(new TestContext(null, selected.Split(',')), args, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
