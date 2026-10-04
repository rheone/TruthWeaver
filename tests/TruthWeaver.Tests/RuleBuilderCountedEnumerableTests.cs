namespace TruthWeaver.Tests;

using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The <see cref="IEnumerable{T}"/> overloads of the counted <see cref="RuleBuilder"/> operators (<c>Between</c>,
/// <c>AtLeast</c>, <c>AtMost</c>, <c>Exactly</c>; k3-followups 22). They build the same node as the <c>params</c> overloads and
/// never fold, because a counted operator has no identity constant.
/// </summary>
public sealed class RuleBuilderCountedEnumerableTests
{
    private static readonly RuleBuilder A = RuleBuilder.Predicate("a");
    private static readonly RuleBuilder B = RuleBuilder.Predicate("b");
    private static readonly RuleBuilder C = RuleBuilder.Predicate("c");

    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    public static TheoryData<string> Operators => ["Between", "AtLeast", "AtMost", "Exactly"];

    /// <summary>A many-item sequence builds exactly the node the params overload builds.</summary>
    [Theory]
    [MemberData(nameof(Operators))]
    public void Enumerable_overload_with_many_items_matches_the_params_overload_Test(string op)
    {
        RuleBuilder viaEnumerable = Build(op, [A, B, C]);
        RuleBuilder viaParams = BuildParams(op, A, B, C);

        Assert.Equal(viaParams.ToJson(), viaEnumerable.ToJson());
    }

    /// <summary>
    /// Empty and one-item sequences are not folded: the node is built as-is and gets the same compile diagnostics as the
    /// params overload.
    /// </summary>
    [Theory]
    [MemberData(nameof(Operators))]
    public void Enumerable_overload_with_no_items_or_one_item_is_not_folded_and_validates_like_params_Test(string op)
    {
        RuleBuilder[][] shortLists =
        [
            [],
            [A],
        ];

        foreach (RuleBuilder[] items in shortLists)
        {
            RuleBuilder viaEnumerable = Build(op, items);
            RuleBuilder viaParams = BuildParams(op, items);

            Assert.Equal(viaParams.ToJson(), viaEnumerable.ToJson());
            Assert.Equal(Codes(viaParams), Codes(viaEnumerable));
        }
    }

    /// <summary>A threshold that the operands cannot meet is rejected, not folded to a constant.</summary>
    [Fact]
    public void AtLeast_with_an_empty_sequence_and_an_unmeetable_count_is_a_compile_error_Test()
    {
        RuleBuilder builder = RuleBuilder.AtLeast(2, (IEnumerable<RuleBuilder>)[]);

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.Null(result.CompiledRule);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(Codes(RuleBuilder.AtLeast(2)), Codes(builder));
    }

    /// <summary>A lazily evaluated sequence is enumerated exactly once.</summary>
    [Theory]
    [MemberData(nameof(Operators))]
    public void Enumerable_overload_enumerates_a_single_pass_sequence_once_Test(string op)
    {
        int enumerations = 0;
        IEnumerable<RuleBuilder> Source()
        {
            enumerations++;
            yield return A;
            yield return B;
            yield return C;
        }

        string json = Build(op, Source()).ToJson();

        Assert.Equal(1, enumerations);
        Assert.Equal(BuildParams(op, A, B, C).ToJson(), json);
    }

    /// <summary>A null sequence is a programming error and is rejected with its parameter name.</summary>
    [Theory]
    [MemberData(nameof(Operators))]
    public void Enumerable_overload_with_a_null_sequence_throws_ArgumentNullException_Test(string op)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Build(op, null!));

        Assert.Equal("operands", exception.ParamName);
    }

    private static string[] Codes(RuleBuilder builder)
    {
        return [.. builder.Compile(Compiler).Diagnostics.Select(d => d.Code)];
    }

    private static RuleBuilder Build(string op, IEnumerable<RuleBuilder> items)
    {
        return op switch
        {
            "Between" => RuleBuilder.Between(1, 2, items),
            "AtLeast" => RuleBuilder.AtLeast(2, items),
            "AtMost" => RuleBuilder.AtMost(2, items),
            "Exactly" => RuleBuilder.Exactly(2, items),
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }

    private static RuleBuilder BuildParams(string op, params RuleBuilder[] items)
    {
        return op switch
        {
            "Between" => RuleBuilder.Between(1, 2, items),
            "AtLeast" => RuleBuilder.AtLeast(2, items),
            "AtMost" => RuleBuilder.AtMost(2, items),
            "Exactly" => RuleBuilder.Exactly(2, items),
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }
}
