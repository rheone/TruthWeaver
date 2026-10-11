namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Testing;

/// <summary>
/// The internal K3 scalar truth functions, checked over every input (3 single values, 9 pairs) against the independent
/// <see cref="K3Oracle"/>, which defines the connectives from the primitives instead of by table.
/// </summary>
public sealed class K3LogicTests
{
    public static TheoryData<TruthValue> SingleValues => [.. K3Oracle.Values];

    public static TheoryData<TruthValue, TruthValue> Pairs
    {
        get
        {
            TheoryData<TruthValue, TruthValue> data = [];
            foreach (TruthValue left in K3Oracle.Values)
            {
                foreach (TruthValue right in K3Oracle.Values)
                {
                    data.Add(left, right);
                }
            }

            return data;
        }
    }

    /// <summary>Negation swaps True and False and fixes Unknown.</summary>
    [Theory]
    [MemberData(nameof(SingleValues))]
    public void Not_EachValue_MatchesTheOracle_Test(TruthValue value)
    {
        Assert.Equal(K3Oracle.Not(value), K3Logic.Not(value));
    }

    /// <summary>Conjunction matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void And_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.And([left, right]), K3Logic.And(left, right));
    }

    /// <summary>Disjunction matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Or_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.Or([left, right]), K3Logic.Or(left, right));
    }

    /// <summary>Exclusive or matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Xor_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.Xor(left, right), K3Logic.Xor(left, right));
    }

    /// <summary>The biconditional matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Equivalent_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.Equivalent(left, right), K3Logic.Equivalent(left, right));
    }

    /// <summary>Negated conjunction matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Nand_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.Nand(left, right), K3Logic.Nand(left, right));
    }

    /// <summary>Negated disjunction matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Nor_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.Nor(left, right), K3Logic.Nor(left, right));
    }

    /// <summary>Material implication matches the oracle for all nine pairs.</summary>
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Implies_EachPair_MatchesTheOracle_Test(TruthValue left, TruthValue right)
    {
        Assert.Equal(K3Oracle.Implies(left, right), K3Logic.Implies(left, right));
    }

    /// <summary>Parity over every assignment of one to four operands matches the oracle.</summary>
    [Fact]
    public void Parity_EveryAssignmentUpToFourOperands_MatchesTheOracle_Test()
    {
        for (int arity = 1; arity <= 4; arity++)
        {
            foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
            {
                Assert.Equal(K3Oracle.Parity(assignment), K3Logic.Parity(assignment));
            }
        }
    }

    /// <summary>Each inspection kind matches the oracle for all three values, and is never Unknown.</summary>
    [Theory]
    [MemberData(nameof(SingleValues))]
    public void Inspect_EachKindAndValue_MatchesTheOracleAndIsDefinite_Test(TruthValue value)
    {
        Assert.Equal(K3Oracle.IsTrue(value), K3Logic.Inspect(InspectionKind.IsTrue, value));
        Assert.Equal(K3Oracle.IsFalse(value), K3Logic.Inspect(InspectionKind.IsFalse, value));
        Assert.Equal(K3Oracle.IsUnknown(value), K3Logic.Inspect(InspectionKind.IsUnknown, value));
        Assert.Equal(K3Oracle.IsKnown(value), K3Logic.Inspect(InspectionKind.IsKnown, value));
    }

    /// <summary>An undefined inspection kind is a programming error, not an authoring error, so it throws.</summary>
    [Fact]
    public void Inspect_UndefinedKind_Throws_Test()
    {
        Assert.Throws<InvalidOperationException>(() => K3Logic.Inspect((InspectionKind)99, TruthValue.True));
    }
}
