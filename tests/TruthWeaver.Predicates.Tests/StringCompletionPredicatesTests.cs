namespace TruthWeaver.Predicates.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;

/// <summary>Behavior of the string completion members and their <c>NotX</c> twins.</summary>
public class StringCompletionPredicatesTests
{
    private delegate ValueTask<TruthValue> Evaluate(TestContext context, PredicateArguments args, CancellationToken token);

    /// <summary>A no-argument string test returns the documented value for null, empty, whitespace and ordinary strings.</summary>
    [Theory]
    [InlineData("IsEmpty", null, TruthValue.Unknown)]
    [InlineData("IsEmpty", "", TruthValue.True)]
    [InlineData("IsEmpty", "  ", TruthValue.False)]
    [InlineData("IsEmpty", "abc", TruthValue.False)]
    [InlineData("IsNotEmpty", null, TruthValue.Unknown)]
    [InlineData("IsNotEmpty", "", TruthValue.False)]
    [InlineData("IsNotEmpty", "  ", TruthValue.True)]
    [InlineData("IsNotEmpty", "abc", TruthValue.True)]
    [InlineData("IsNotNullOrEmpty", null, TruthValue.False)]
    [InlineData("IsNotNullOrEmpty", "", TruthValue.False)]
    [InlineData("IsNotNullOrEmpty", "  ", TruthValue.True)]
    [InlineData("IsNotNullOrEmpty", "abc", TruthValue.True)]
    [InlineData("IsNullOrWhiteSpace", null, TruthValue.True)]
    [InlineData("IsNullOrWhiteSpace", "", TruthValue.True)]
    [InlineData("IsNullOrWhiteSpace", " \t\n", TruthValue.True)]
    [InlineData("IsNullOrWhiteSpace", "abc", TruthValue.False)]
    [InlineData("IsNotNullOrWhiteSpace", null, TruthValue.False)]
    [InlineData("IsNotNullOrWhiteSpace", "", TruthValue.False)]
    [InlineData("IsNotNullOrWhiteSpace", " \t\n", TruthValue.False)]
    [InlineData("IsNotNullOrWhiteSpace", "abc", TruthValue.True)]
    public async Task NoArgumentMember_NullEmptyWhitespaceOrdinary_ReturnsDocumentedValue_Test(
        string member,
        string? value,
        TruthValue expected
    )
    {
        Evaluate evaluate = NoArgument(member, NullBehavior.Unknown);

        TruthValue result = await evaluate(new TestContext(value), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    /// <summary>With <c>NullBehavior.False</c> a null selection answers a definite False for <c>IsEmpty</c>.</summary>
    [Fact]
    public async Task IsEmpty_NullSelectionWithFalseBehavior_ReturnsFalse_Test()
    {
        Evaluate evaluate = NoArgument("IsEmpty", NullBehavior.False);

        TruthValue result = await evaluate(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>A twin is the K3 complement of its positive form for every selection, with Unknown staying Unknown.</summary>
    [Theory]
    [InlineData("IsEmpty", "IsNotEmpty")]
    [InlineData("IsNullOrEmpty", "IsNotNullOrEmpty")]
    [InlineData("IsNullOrWhiteSpace", "IsNotNullOrWhiteSpace")]
    public async Task NoArgumentTwin_AnySelection_IsK3ComplementOfPositive_Test(string positive, string twin)
    {
        foreach (string? value in new string?[] { null, string.Empty, " ", "abc" })
        {
            TruthValue x = await NoArgument(positive, NullBehavior.Unknown)(
                new TestContext(value),
                PredicateArguments.Empty,
                CancellationToken.None
            );
            TruthValue notX = await NoArgument(twin, NullBehavior.Unknown)(
                new TestContext(value),
                PredicateArguments.Empty,
                CancellationToken.None
            );

            Assert.Equal(Complement(x), notX);
        }
    }

    /// <summary>Comparison twins agree with the K3 complement of the positive form for null, match and non-match.</summary>
    [Theory]
    [InlineData("Equals", "NotEqual", "value", "abc")]
    [InlineData("Contains", "NotContains", "value", "b")]
    [InlineData("Matches", "NotMatches", "pattern", "^a")]
    public async Task ComparisonTwin_AnySelection_IsK3ComplementOfPositive_Test(
        string positive,
        string twin,
        string argName,
        string argument
    )
    {
        foreach (string? value in new string?[] { null, "abc", "xyz" })
        {
            TruthValue x = await Comparison(positive)(new TestContext(value), Args(argName, argument), CancellationToken.None);
            TruthValue notX = await Comparison(twin)(new TestContext(value), Args(argName, argument), CancellationToken.None);

            Assert.Equal(Complement(x), notX);
        }
    }

    /// <summary>The comparison twins use ordinal, case-sensitive comparison.</summary>
    [Fact]
    public async Task NotEqual_DifferentCase_ReturnsTrue_Test()
    {
        Evaluate evaluate = Comparison("NotEqual");

        TruthValue result = await evaluate(new TestContext("Alice"), Args("value", "alice"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>A null selection answers Unknown by default, so a negation never turns a missing value into True.</summary>
    [Fact]
    public async Task NotContains_NullSelection_DefaultsToUnknown_Test()
    {
        Evaluate evaluate = Comparison("NotContains");

        TruthValue result = await evaluate(new TestContext(null), Args("value", "x"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>The host can choose a definite False for a null selection, never True.</summary>
    [Fact]
    public async Task NotEqual_NullSelectionWithFalseBehavior_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            StringPredicates.NotEqual<TestContext>("ne", c => c.Value, nullBehavior: NullBehavior.False);

        TruthValue result = await evaluate(new TestContext(null), Args("value", "x"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>An invalid pattern throws at evaluation time, which the evaluator turns into Unknown plus a Fault.</summary>
    [Fact]
    public Task NotMatches_InvalidPattern_ThrowsSoTheEvaluatorRecordsAFault_Test()
    {
        Evaluate evaluate = Comparison("NotMatches");

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args("pattern", "("), CancellationToken.None)
        );
    }

    /// <summary>Every new member carries a description, and the comparison members declare one String argument.</summary>
    [Fact]
    public void Schemas_NewMembers_HaveDescriptionsAndArguments_Test()
    {
        PredicateSchema[] noArgs =
        [
            StringPredicates.IsEmpty<TestContext>("a", c => c.Value).Schema,
            StringPredicates.IsNotEmpty<TestContext>("a", c => c.Value).Schema,
            StringPredicates.IsNotNullOrEmpty<TestContext>("a", c => c.Value).Schema,
            StringPredicates.IsNullOrWhiteSpace<TestContext>("a", c => c.Value).Schema,
            StringPredicates.IsNotNullOrWhiteSpace<TestContext>("a", c => c.Value).Schema,
        ];
        PredicateSchema[] oneArg =
        [
            StringPredicates.NotContains<TestContext>("a", c => c.Value).Schema,
            StringPredicates.NotEqual<TestContext>("a", c => c.Value).Schema,
            RegexPredicates.NotMatches<TestContext>("a", c => c.Value).Schema,
        ];

        Assert.All(noArgs, s => Assert.Empty(s.Arguments));
        Assert.All(oneArg, s => Assert.Equal(LiteralKind.String, Assert.Single(s.Arguments).Type));
        Assert.All(noArgs.Concat(oneArg), s => Assert.False(string.IsNullOrWhiteSpace(s.Description)));
    }

    private static TruthValue Complement(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    private static Evaluate NoArgument(string member, NullBehavior nullBehavior)
    {
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> f = member switch
        {
            "IsEmpty" => StringPredicates.IsEmpty<TestContext>("p", c => c.Value, nullBehavior: nullBehavior).Evaluate,
            "IsNotEmpty" => StringPredicates.IsNotEmpty<TestContext>("p", c => c.Value, nullBehavior: nullBehavior).Evaluate,
            "IsNullOrEmpty" => StringPredicates.IsNullOrEmpty<TestContext>("p", c => c.Value).Evaluate,
            "IsNotNullOrEmpty" => StringPredicates.IsNotNullOrEmpty<TestContext>("p", c => c.Value).Evaluate,
            "IsNullOrWhiteSpace" => StringPredicates.IsNullOrWhiteSpace<TestContext>("p", c => c.Value).Evaluate,
            "IsNotNullOrWhiteSpace" => StringPredicates.IsNotNullOrWhiteSpace<TestContext>("p", c => c.Value).Evaluate,
            _ => throw new ArgumentException(member),
        };
        return new Evaluate(f);
    }

    private static Evaluate Comparison(string member)
    {
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> f = member switch
        {
            "Equals" => StringPredicates.Equals<TestContext>("p", c => c.Value, nullBehavior: NullBehavior.Unknown).Evaluate,
            "NotEqual" => StringPredicates.NotEqual<TestContext>("p", c => c.Value).Evaluate,
            "Contains" => StringPredicates
                .Contains<TestContext>("p", c => c.Value, nullBehavior: NullBehavior.Unknown)
                .Evaluate,
            "NotContains" => StringPredicates.NotContains<TestContext>("p", c => c.Value).Evaluate,
            "Matches" => RegexPredicates.Matches<TestContext>("p", c => c.Value, nullBehavior: NullBehavior.Unknown).Evaluate,
            "NotMatches" => RegexPredicates.NotMatches<TestContext>("p", c => c.Value).Evaluate,
            _ => throw new ArgumentException(member),
        };
        return new Evaluate(f);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
