namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

/// <summary>
/// Pins the host-chosen <see cref="NullBehavior"/> option on every built-in string, regex and collection
/// predicate factory: the default keeps a definite False for a null selected value, and the Unknown
/// option answers Unknown (never a thrown fault) for the same input.
/// </summary>
public class NullBehaviorTests
{
    /// <summary>Gets one entry per factory that takes a selector and a null-behavior option.</summary>
    public static TheoryData<string> FactoryNames =>
        ["Equals", "EqualsIgnoreCase", "StartsWith", "EndsWith", "Contains", "EqualsConfigurable", "Matches", "SetEquals"];

    /// <summary>
    /// A null selected value with no explicit option stays a definite False for every existing member.
    /// SetEquals is checked against a non-empty literal, since a null collection is an empty set there.
    /// </summary>
    [Theory]
    [MemberData(nameof(FactoryNames))]
    public async Task Evaluate_NullSelectedValueWithDefaultOption_ReturnsFalse_Test(string factoryName)
    {
        TruthValue result = await EvaluateNullAsync(factoryName, nullBehavior: null);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>An explicit <see cref="NullBehavior.False"/> behaves exactly like the default.</summary>
    [Theory]
    [MemberData(nameof(FactoryNames))]
    public async Task Evaluate_NullSelectedValueWithFalseOption_ReturnsFalse_Test(string factoryName)
    {
        TruthValue result = await EvaluateNullAsync(factoryName, NullBehavior.False);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>
    /// <see cref="NullBehavior.Unknown"/> makes a null selected value Unknown, returned directly by the
    /// predicate rather than thrown as a fault.
    /// </summary>
    [Theory]
    [MemberData(nameof(FactoryNames))]
    public async Task Evaluate_NullSelectedValueWithUnknownOption_ReturnsUnknownWithoutThrowing_Test(string factoryName)
    {
        TruthValue result = await EvaluateNullAsync(factoryName, NullBehavior.Unknown);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>
    /// The Unknown option only changes the null case: a non-null selected value still gets a definite
    /// answer from the comparison.
    /// </summary>
    [Fact]
    public async Task Equals_NonNullSelectedValueWithUnknownOption_ReturnsDefiniteAnswer_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.Equals<TestContext>("isAlice", c => c.Value, nullBehavior: NullBehavior.Unknown);

        TruthValue match = await evaluate(new TestContext("alice"), Args("value", "alice"), CancellationToken.None);
        TruthValue mismatch = await evaluate(new TestContext("bob"), Args("value", "alice"), CancellationToken.None);

        Assert.Equal(TruthValue.True, match);
        Assert.Equal(TruthValue.False, mismatch);
    }

    /// <summary>
    /// A non-null collection under the Unknown option is still compared as a set; only a null
    /// collection (not an empty one) becomes Unknown.
    /// </summary>
    [Fact]
    public async Task SetEquals_EmptySelectedCollectionWithUnknownOption_ReturnsTrueForEmptyLiteral_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("same", c => c.Values, nullBehavior: NullBehavior.Unknown);

        TruthValue result = await evaluate(new TestContext(null, []), ArrayArgs(), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    private static async Task<TruthValue> EvaluateNullAsync(string factoryName, NullBehavior? nullBehavior)
    {
        NullBehavior behavior = nullBehavior ?? default;
        bool useDefault = nullBehavior is null;
        TestContext context = new(null, null);

        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) = factoryName switch
        {
            "Equals" => useDefault
                ? StringPredicates.Equals<TestContext>("p", c => c.Value)
                : StringPredicates.Equals<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "EqualsIgnoreCase" => useDefault
                ? StringPredicates.EqualsIgnoreCase<TestContext>("p", c => c.Value)
                : StringPredicates.EqualsIgnoreCase<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "StartsWith" => useDefault
                ? StringPredicates.StartsWith<TestContext>("p", c => c.Value)
                : StringPredicates.StartsWith<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "EndsWith" => useDefault
                ? StringPredicates.EndsWith<TestContext>("p", c => c.Value)
                : StringPredicates.EndsWith<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "Contains" => useDefault
                ? StringPredicates.Contains<TestContext>("p", c => c.Value)
                : StringPredicates.Contains<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "EqualsConfigurable" => useDefault
                ? StringPredicates.EqualsConfigurable<TestContext>("p", c => c.Value)
                : StringPredicates.EqualsConfigurable<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "Matches" => useDefault
                ? RegexPredicates.Matches<TestContext>("p", c => c.Value)
                : RegexPredicates.Matches<TestContext>("p", c => c.Value, nullBehavior: behavior),
            "SetEquals" => useDefault
                ? CollectionPredicates.SetEquals<TestContext>("p", c => c.Values)
                : CollectionPredicates.SetEquals<TestContext>("p", c => c.Values, nullBehavior: behavior),
            _ => throw new ArgumentOutOfRangeException(nameof(factoryName), factoryName, null),
        };

        PredicateArguments args = factoryName switch
        {
            "Matches" => Args("pattern", "a"),
            "SetEquals" => ArrayArgs("a"),
            "EqualsConfigurable" => new PredicateArguments(
                new Dictionary<string, LiteralValue>
                {
                    ["value"] = LiteralValue.OfString("a"),
                    ["ignoreCase"] = LiteralValue.OfBoolean(true),
                    ["trim"] = LiteralValue.OfBoolean(false),
                }
            ),
            _ => Args("value", "a"),
        };

        return await evaluate(context, args, CancellationToken.None);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }

    private static PredicateArguments ArrayArgs(params string[] values)
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.String, values.Select(LiteralValue.OfString));
        return new PredicateArguments(new Dictionary<string, LiteralValue> { ["values"] = array });
    }
}
