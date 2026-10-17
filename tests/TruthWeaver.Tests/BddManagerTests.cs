namespace TruthWeaver.Tests;

using TruthWeaver.Analysis;

/// <summary>Ticket 02: direct unit tests for the hand-rolled ROBDD engine.</summary>
public sealed class BddManagerTests
{
    /// <summary>A variable returns a stable node id on repeated calls.</summary>
    [Fact]
    public void Variable_RepeatedCalls_ReturnStableNodeId_Test()
    {
        BddManager bdd = new();

        int first = bdd.Variable(0);
        int second = bdd.Variable(0);

        Assert.Equal(first, second);
    }

    /// <summary>The conjunction of a variable and its negation reduces to false.</summary>
    [Fact]
    public void And_VariableAndNegation_ReducesToFalse_Test()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.And(x, bdd.Not(x));

        Assert.Equal(BddManager.False, result);
    }

    /// <summary>The disjunction of a variable and its negation reduces to true.</summary>
    [Fact]
    public void Or_VariableAndNegation_ReducesToTrue_Test()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.Or(x, bdd.Not(x));

        Assert.Equal(BddManager.True, result);
    }

    /// <summary>The conjunction of a variable with itself reduces to the variable.</summary>
    [Fact]
    public void And_VariableWithItself_ReducesToItself_Test()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.And(x, x);

        Assert.Equal(x, result);
    }

    /// <summary>The disjunction of a variable with itself reduces to the variable.</summary>
    [Fact]
    public void Or_VariableWithItself_ReducesToItself_Test()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.Or(x, x);

        Assert.Equal(x, result);
    }

    /// <summary>Structurally equal BDDs built independently share a node id.</summary>
    [Fact]
    public void NodeId_StructurallyEqualBdds_ShareNodeId_Test()
    {
        BddManager bdd = new();

        int first = bdd.And(bdd.Variable(0), bdd.Variable(1));
        int second = bdd.And(bdd.Variable(0), bdd.Variable(1));

        Assert.Equal(first, second);
    }

    /// <summary>Exclusive or matches its truth table.</summary>
    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void Xor_TruthTable_MatchesTruthTable_Test(bool a, bool b, bool expected)
    {
        BddManager bdd = new();
        int x = a ? BddManager.True : BddManager.False;
        int y = b ? BddManager.True : BddManager.False;

        int result = bdd.Xor(x, y);

        Assert.Equal(expected ? BddManager.True : BddManager.False, result);
    }

    /// <summary>If-then-else over three independent variables matches its AND, OR and NOT expansion.</summary>
    [Fact]
    public void Ite_ThreeIndependentVariables_MatchesAndOrNotExpansion_Test()
    {
        // BddManager.Ite(i, t, e) is defined semantically as "(i AND t) OR (NOT i AND e)". Because
        // ROBDD nodes are uniquified per (variable, low, high) under a fixed variable ordering, two
        // different constructions of the *same* boolean function are guaranteed to produce the
        // identical node id - so comparing node ids here is equivalent to checking every one of the
        // 8 possible assignments of i/t/e against the brute-force truth table.
        BddManager bdd = new();
        int i = bdd.Variable(0);
        int t = bdd.Variable(1);
        int e = bdd.Variable(2);

        int viaIte = bdd.Ite(i, t, e);
        int viaExpansion = bdd.Or(bdd.And(i, t), bdd.And(bdd.Not(i), e));

        Assert.Equal(viaExpansion, viaIte);
    }

    /// <summary>Repeated if-then-else calls with the same operands stay correct under heavy reuse.</summary>
    [Fact]
    public void Ite_RepeatedSameOperands_RemainsCorrectUnderReuse_Test()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);
        int y = bdd.Variable(1);
        int z = bdd.Variable(2);

        int first = bdd.Ite(x, y, z);
        int last = first;
        for (int i = 0; i < 1000; i++)
        {
            last = bdd.Ite(x, y, z);
        }

        Assert.Equal(first, last);

        int expansion = bdd.Or(bdd.And(x, y), bdd.And(bdd.Not(x), z));
        Assert.Equal(expansion, last);
    }
}
