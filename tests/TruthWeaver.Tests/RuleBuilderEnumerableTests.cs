namespace TruthWeaver.Tests;

using TruthWeaver.Building;

/// <summary>The <see cref="IEnumerable{T}"/> overloads of the n-ary <see cref="RuleBuilder"/> factories and their 0/1-item folding.</summary>
public sealed class RuleBuilderEnumerableTests
{
    private static readonly RuleBuilder A = RuleBuilder.Predicate("a");
    private static readonly RuleBuilder B = RuleBuilder.Predicate("b");
    private static readonly RuleBuilder C = RuleBuilder.Predicate("c");

    public static TheoryData<string> Operators => ["And", "Or", "Parity", "Any", "All", "None", "ExactlyOne", "Coalesce"];

    public static TheoryData<string> OperatorsWithAnIdentity => ["And", "Or", "Any", "All", "None"];

    /// <summary>An empty sequence folds to the operator's identity constant instead of a rejected zero-operand node.</summary>
    [Theory]
    [InlineData("And", "{\"const\":true}")]
    [InlineData("Or", "{\"const\":false}")]
    [InlineData("Any", "{\"const\":false}")]
    [InlineData("All", "{\"const\":true}")]
    [InlineData("None", "{\"const\":true}")]
    public void Enumerable_overload_with_no_items_folds_to_the_identity_constant_Test(string op, string expectedJson)
    {
        RuleBuilder result = Build(op, []);

        Assert.Equal(expectedJson, result.ToJson());
    }

    /// <summary>A single-item sequence folds to that operand, because a one-operand node would be rejected.</summary>
    [Theory]
    [MemberData(nameof(OperatorsWithAnIdentity))]
    public void Enumerable_overload_with_one_item_returns_that_operand_Test(string op)
    {
        // NONE of one operand is the operand's negation; every other operator is the identity on one operand.
        string expected = op == "None" ? RuleBuilder.Not(A).ToJson() : A.ToJson();

        RuleBuilder result = Build(op, [A]);

        Assert.Equal(expected, result.ToJson());
    }

    /// <summary>An operator with no identity builds the node the params overload builds for fewer than two items, so the compiler rejects it.</summary>
    [Theory]
    [InlineData("Parity")]
    [InlineData("ExactlyOne")]
    [InlineData("Coalesce")]
    public void Enumerable_overload_of_an_operator_with_no_identity_does_not_fold_Test(string op)
    {
        Assert.Equal(BuildParams(op).ToJson(), Build(op, []).ToJson());
        Assert.Equal(BuildParams(op, A).ToJson(), Build(op, [A]).ToJson());
    }

    /// <summary>Two or more items build exactly the node the params overload builds.</summary>
    [Theory]
    [MemberData(nameof(Operators))]
    public void Enumerable_overload_with_many_items_matches_the_params_overload_Test(string op)
    {
        RuleBuilder viaEnumerable = Build(op, [A, B, C]);
        RuleBuilder viaParams = BuildParams(op, A, B, C);

        Assert.Equal(viaParams.ToJson(), viaEnumerable.ToJson());
    }

    /// <summary>A lazily evaluated sequence is enumerated once, so single-pass sources are safe.</summary>
    [Fact]
    public void And_with_a_single_pass_sequence_enumerates_it_once_Test()
    {
        int enumerations = 0;
        IEnumerable<RuleBuilder> Source()
        {
            enumerations++;
            yield return A;
            yield return B;
        }

        string json = RuleBuilder.And(Source()).ToJson();

        Assert.Equal(1, enumerations);
        Assert.Equal(RuleBuilder.And(A, B).ToJson(), json);
    }

    /// <summary>A null sequence is a programming error and is rejected with its parameter name.</summary>
    [Fact]
    public void And_with_a_null_sequence_throws_ArgumentNullException_Test()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            RuleBuilder.And((IEnumerable<RuleBuilder>)null!)
        );

        Assert.Equal("operands", exception.ParamName);
    }

    private static RuleBuilder Build(string op, IEnumerable<RuleBuilder> items)
    {
        return op switch
        {
            "And" => RuleBuilder.And(items),
            "Or" => RuleBuilder.Or(items),
            "Parity" => RuleBuilder.Parity(items),
            "Any" => RuleBuilder.Any(items),
            "All" => RuleBuilder.All(items),
            "None" => RuleBuilder.None(items),
            "ExactlyOne" => RuleBuilder.ExactlyOne(items),
            "Coalesce" => RuleBuilder.Coalesce(items),
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }

    private static RuleBuilder BuildParams(string op, params RuleBuilder[] items)
    {
        return op switch
        {
            "And" => RuleBuilder.And(items),
            "Or" => RuleBuilder.Or(items),
            "Parity" => RuleBuilder.Parity(items),
            "Any" => RuleBuilder.Any(items),
            "All" => RuleBuilder.All(items),
            "None" => RuleBuilder.None(items),
            "ExactlyOne" => RuleBuilder.ExactlyOne(items),
            "Coalesce" => RuleBuilder.Coalesce(items),
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }
}
