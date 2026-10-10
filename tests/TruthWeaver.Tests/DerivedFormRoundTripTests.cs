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

    /// <summary>Single indices over the rich pool.</summary>
    public static TheoryData<int> RichSingles()
    {
        TheoryData<int> data = [];
        for (int i = 0; i < RichPool().Length; i++)
        {
            data.Add(i);
        }

        return data;
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

    /// <summary>The Parity matcher recovers the operands of the form the builder produced, for three or more operands.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void ParityFormTryMatch_BuiltForm_RecoversOperands_Test(int count)
    {
        EquatableArray<Expression> operands = Operands(count);
        OrExpression built = Assert.IsType<OrExpression>(ParityForm.Build(operands));

        Assert.True(ParityForm.TryMatch(built.Operands, out EquatableArray<Expression> recovered));
        Assert.Equal(operands, recovered);
    }

    /// <summary>Two operands build a single Exactly(1), which is not an OR, so there is no form to recognise.</summary>
    [Fact]
    public void ParityFormBuild_TwoOperands_IsASingleExactlyOne_Test()
    {
        Expression built = ParityForm.Build(Operands(2));

        Assert.Equal(new ThresholdExpression(ThresholdComparison.Exactly, 1, Operands(2)), built);
    }

    /// <summary>The Parity matcher rejects an OR that skips an odd count.</summary>
    [Fact]
    public void ParityFormTryMatch_MissingOddCount_IsNotMatched_Test()
    {
        EquatableArray<Expression> operands = Operands(5);
        EquatableArray<Expression> gapped = new([
            new ThresholdExpression(ThresholdComparison.Exactly, 1, operands),
            new ThresholdExpression(ThresholdComparison.Exactly, 5, operands),
        ]);

        Assert.False(ParityForm.TryMatch(gapped, out _));
    }

    /// <summary>Compressing an expanded PARITY of three or more operands gives the PARITY back.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Compress_ExpandedParity_RecoversParity_Test(int count)
    {
        Expression parity = new ParityExpression(Operands(count));

        Assert.Equal(parity, Compressor.Compress(PrimitiveExpander.Expand(parity)));
    }

    /// <summary>The IsKnown matcher recovers the operand of the form the builder produced, in either member order.</summary>
    [Theory]
    [MemberData(nameof(RichSingles))]
    public void InspectionFormTryMatchPair_IsKnownForm_RecoversOperand_Test(int operandIndex)
    {
        Expression operand = RichPool()[operandIndex];
        OrExpression built = (OrExpression)InspectionForm.Build(InspectionKind.IsKnown, operand);
        EquatableArray<Expression> swapped = new([built.Operands[1], built.Operands[0]]);

        Assert.True(InspectionForm.TryMatchPair(built.Operands, InspectionKind.IsKnown, out Expression? recovered));
        Assert.Equal(operand, recovered);
        Assert.True(InspectionForm.TryMatchPair(swapped, InspectionKind.IsKnown, out recovered));
        Assert.Equal(operand, recovered);
    }

    /// <summary>The IsUnknown matcher recovers the operand of the form the builder produced, in either member order.</summary>
    [Theory]
    [MemberData(nameof(RichSingles))]
    public void InspectionFormTryMatchPair_IsUnknownForm_RecoversOperand_Test(int operandIndex)
    {
        Expression operand = RichPool()[operandIndex];
        AndExpression built = (AndExpression)InspectionForm.Build(InspectionKind.IsUnknown, operand);
        EquatableArray<Expression> swapped = new([built.Operands[1], built.Operands[0]]);

        Assert.True(InspectionForm.TryMatchPair(built.Operands, InspectionKind.IsUnknown, out Expression? recovered));
        Assert.Equal(operand, recovered);
        Assert.True(InspectionForm.TryMatchPair(swapped, InspectionKind.IsUnknown, out recovered));
        Assert.Equal(operand, recovered);
    }

    /// <summary>The IsKnown matcher does not accept the IsUnknown form, because the fallback constants differ.</summary>
    [Fact]
    public void InspectionFormTryMatchPair_WrongFallback_IsNotMatched_Test()
    {
        AndExpression unknown = (AndExpression)InspectionForm.Build(InspectionKind.IsUnknown, Term("a"));

        Assert.False(InspectionForm.TryMatchPair(unknown.Operands, InspectionKind.IsKnown, out _));
    }

    /// <summary>Compressing an expanded IsKnown or IsUnknown gives the inspection back.</summary>
    [Theory]
    [InlineData(InspectionKind.IsKnown, 0)]
    [InlineData(InspectionKind.IsKnown, 1)]
    [InlineData(InspectionKind.IsUnknown, 0)]
    [InlineData(InspectionKind.IsUnknown, 1)]
    public void Compress_ExpandedPairedInspection_RecoversInspection_Test(InspectionKind kind, int operandIndex)
    {
        Expression inspection = new InspectionExpression(kind, TermPool()[operandIndex]);

        Assert.Equal(inspection, Compressor.Compress(PrimitiveExpander.Expand(inspection)));
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

    private static EquatableArray<Expression> Operands(int count)
    {
        return new EquatableArray<Expression>([.. Enumerable.Range(0, count).Select(i => (Expression)Term($"p{i}"))]);
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
