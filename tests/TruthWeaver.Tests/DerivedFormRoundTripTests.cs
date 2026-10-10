namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Rewriting;

/// <summary>
/// Checks that each derived-operator form recognises exactly what it builds, and that compressing an expanded operator
/// recovers the operator, over a generated set of operand shapes. Theory data holds pool indices, not expressions,
/// because <see cref="Expression"/> is not serializable for Test Explorer.
/// </summary>
public sealed class DerivedFormRoundTripTests
{
    /// <summary>Index pairs over the rich pool, every ordered pair including a repeated operand.</summary>
    public static TheoryData<int, int> RichPairs()
    {
        return Pairs(RichPool().Length);
    }

    /// <summary>Index triples over the rich pool, every ordered triple including repeats.</summary>
    public static TheoryData<int, int, int> RichTriples()
    {
        return Triples(RichPool().Length);
    }

    /// <summary>Index pairs over the term pool.</summary>
    public static TheoryData<int, int> TermPairs()
    {
        return Pairs(TermPool().Length);
    }

    /// <summary>Index triples over the term pool.</summary>
    public static TheoryData<int, int, int> TermTriples()
    {
        return Triples(TermPool().Length);
    }

    /// <summary>The XOR matcher recovers the operands of the form the builder produced.</summary>
    [Theory]
    [MemberData(nameof(RichPairs))]
    public void XorFormTryMatch_BuiltForm_RecoversOperands_Test(int leftIndex, int rightIndex)
    {
        Expression left = RichPool()[leftIndex];
        Expression right = RichPool()[rightIndex];
        OrExpression built = XorForm.Build(left, right);

        Assert.True(XorForm.TryMatch(built.Operands, out Expression? l, out Expression? r));
        Assert.Equal(left, l);
        Assert.Equal(right, r);
    }

    /// <summary>The XOR matcher accepts the two disjuncts swapped, because OR is commutative.</summary>
    [Fact]
    public void XorFormTryMatch_SwappedDisjuncts_RecoversOperands_Test()
    {
        OrExpression built = XorForm.Build(Term("a"), Term("b"));
        EquatableArray<Expression> swapped = new([built.Operands[1], built.Operands[0]]);

        Assert.True(XorForm.TryMatch(swapped, out Expression? l, out Expression? r));
        Assert.Equal(Term("a"), l);
        Assert.Equal(Term("b"), r);
    }

    /// <summary>The XOR and EQUIVALENT matchers do not accept each other's forms.</summary>
    [Fact]
    public void TryMatch_XorAndEquivalentForms_AreDistinct_Test()
    {
        OrExpression xor = XorForm.Build(Term("a"), Term("b"));
        OrExpression equivalent = EquivalentForm.Build(Term("a"), Term("b"));

        Assert.False(EquivalentForm.TryMatch(xor.Operands, out _, out _));
        Assert.False(XorForm.TryMatch(equivalent.Operands, out _, out _));
    }

    /// <summary>The EQUIVALENT matcher recovers the operands of the form the builder produced.</summary>
    [Theory]
    [MemberData(nameof(RichPairs))]
    public void EquivalentFormTryMatch_BuiltForm_RecoversOperands_Test(int leftIndex, int rightIndex)
    {
        Expression left = RichPool()[leftIndex];
        Expression right = RichPool()[rightIndex];
        OrExpression built = EquivalentForm.Build(left, right);

        Assert.True(EquivalentForm.TryMatch(built.Operands, out Expression? l, out Expression? r));
        Assert.Equal(left, l);
        Assert.Equal(right, r);
    }

    /// <summary>The If matcher recovers the three operands of the form the builder produced.</summary>
    [Theory]
    [MemberData(nameof(RichTriples))]
    public void IfFormTryMatch_BuiltForm_RecoversOperands_Test(int conditionIndex, int whenTrueIndex, int whenFalseIndex)
    {
        Expression condition = RichPool()[conditionIndex];
        Expression whenTrue = RichPool()[whenTrueIndex];
        Expression whenFalse = RichPool()[whenFalseIndex];
        OrExpression built = IfForm.Build(condition, whenTrue, whenFalse);

        Assert.True(IfForm.TryMatch(built.Operands, out Expression? c, out Expression? t, out Expression? f));
        Assert.Equal(condition, c);
        Assert.Equal(whenTrue, t);
        Assert.Equal(whenFalse, f);
    }

    /// <summary>Compressing an expanded XOR gives the XOR back.</summary>
    [Theory]
    [MemberData(nameof(TermPairs))]
    public void Compress_ExpandedXor_RecoversXor_Test(int leftIndex, int rightIndex)
    {
        Expression xor = new XorExpression(TermPool()[leftIndex], TermPool()[rightIndex]);

        Assert.Equal(xor, Compressor.Compress(PrimitiveExpander.Expand(xor)));
    }

    /// <summary>Compressing an expanded EQUIVALENT gives the EQUIVALENT back.</summary>
    [Theory]
    [MemberData(nameof(TermPairs))]
    public void Compress_ExpandedEquivalent_RecoversEquivalent_Test(int leftIndex, int rightIndex)
    {
        Expression equivalent = new EquivalentExpression(TermPool()[leftIndex], TermPool()[rightIndex]);

        Assert.Equal(equivalent, Compressor.Compress(PrimitiveExpander.Expand(equivalent)));
    }

    /// <summary>Compressing an expanded If gives the If back.</summary>
    [Theory]
    [MemberData(nameof(TermTriples))]
    public void Compress_ExpandedIf_RecoversIf_Test(int conditionIndex, int whenTrueIndex, int whenFalseIndex)
    {
        Expression conditional = new IfExpression(
            TermPool()[conditionIndex],
            TermPool()[whenTrueIndex],
            TermPool()[whenFalseIndex]
        );

        Assert.Equal(conditional, Compressor.Compress(PrimitiveExpander.Expand(conditional)));
    }

    /// <summary>Operand shapes for the matcher tests: leaves, a negation, a constant, an operator and a nested pair.</summary>
    private static Expression[] RichPool()
    {
        Expression a = Term("a");
        Expression b = Term("b");
        Expression c = Term("c");
        return
        [
            a,
            b,
            new NotExpression(a),
            new ConstantExpression(TruthValue.Unknown),
            new AndExpression(new EquatableArray<Expression>([a, c])),
            new OrExpression(new EquatableArray<Expression>([new NotExpression(b), c])),
        ];
    }

    /// <summary>
    /// Operand shapes the compressor leaves alone: plain terms. Richer shapes (constants, negations, operators) have their
    /// own compression rules, so a round trip over them need not return the original operand.
    /// </summary>
    private static Expression[] TermPool()
    {
        return [Term("a"), Term("b"), Term("c")];
    }

    private static TheoryData<int, int> Pairs(int size)
    {
        TheoryData<int, int> data = [];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                data.Add(x, y);
            }
        }

        return data;
    }

    private static TheoryData<int, int, int> Triples(int size)
    {
        TheoryData<int, int, int> data = [];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    data.Add(x, y, z);
                }
            }
        }

        return data;
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
