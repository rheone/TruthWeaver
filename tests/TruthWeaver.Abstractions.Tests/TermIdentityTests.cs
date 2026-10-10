namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TermIdentityTests
{
    /// <summary>Arguments supplied in a different source order produce the same identity.</summary>
    [Fact]
    public void TermIdentity_ArgumentsInDifferentOrder_ProduceSameIdentity_Test()
    {
        TermIdentity first = new(
            "hasEnoughApprovals",
            [
                new KeyValuePair<string, LiteralValue>("minCount", LiteralValue.OfInt64(2)),
                new KeyValuePair<string, LiteralValue>("withinHours", LiteralValue.OfInt64(24)),
            ]
        );
        TermIdentity second = new(
            "hasEnoughApprovals",
            [
                new KeyValuePair<string, LiteralValue>("withinHours", LiteralValue.OfInt64(24)),
                new KeyValuePair<string, LiteralValue>("minCount", LiteralValue.OfInt64(2)),
            ]
        );

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>A different argument value produces a different identity.</summary>
    [Fact]
    public void TermIdentity_DifferentArgumentValue_ProducesDifferentIdentity_Test()
    {
        TermIdentity role1 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity role2 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Z"))]);

        Assert.NotEqual(role1, role2);
        Assert.True(role1 != role2);
    }

    /// <summary>Argument values compare case sensitively.</summary>
    [Fact]
    public void Equals_ArgumentValues_CompareCaseSensitively_Test()
    {
        TermIdentity lower = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("y"))]);
        TermIdentity upper = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.NotEqual(lower, upper);
    }

    /// <summary>A zero argument term renders as just its predicate name.</summary>
    [Fact]
    public void ToString_NoArguments_RendersPredicateNameOnly_Test()
    {
        TermIdentity term = new("isManager", []);

        Assert.Equal("isManager", term.ToString());
    }

    /// <summary>A term renders its arguments sorted by name, whatever the construction order.</summary>
    [Fact]
    public void ToString_ArgumentsInAnyOrder_RendersSortedByName_Test()
    {
        TermIdentity term = new(
            "hasEnoughApprovals",
            [
                new KeyValuePair<string, LiteralValue>("withinHours", LiteralValue.OfInt64(24)),
                new KeyValuePair<string, LiteralValue>("minCount", LiteralValue.OfInt64(2)),
            ]
        );

        Assert.Equal("hasEnoughApprovals(minCount: 2, withinHours: 24)", term.ToString());
    }

    /// <summary>A null is never equal to a term identity.</summary>
    [Fact]
    public void Equals_Null_ReturnsFalse_Test()
    {
        TermIdentity term = new("isManager", []);
        TermIdentity? nullTerm = null;

        Assert.False(term.Equals(null));
        Assert.False(term == nullTerm);
        Assert.False(nullTerm == term);
    }

    /// <summary>Equals returns true for the same reference.</summary>
    [Fact]
    public void Equals_SameReference_ReturnsTrue_Test()
    {
        TermIdentity term = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.True(term.Equals(term));
    }

    /// <summary>The equality operator returns true for equal identities.</summary>
    [Fact]
    public void OperatorEquals_EqualIdentities_ReturnsTrue_Test()
    {
        TermIdentity first = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity second = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.True(first == second);
        Assert.False(first != second);
    }

    /// <summary>The equality operator returns false for a different predicate name.</summary>
    [Fact]
    public void OperatorEquals_DifferentPredicateName_ReturnsFalse_Test()
    {
        TermIdentity role = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity isManager = new("isManager", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.False(role == isManager);
        Assert.True(role != isManager);
    }

    /// <summary>The equality operator returns false for a different argument value.</summary>
    [Fact]
    public void OperatorEquals_DifferentArgumentValue_ReturnsFalse_Test()
    {
        TermIdentity role1 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity role2 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Z"))]);

        Assert.False(role1 == role2);
        Assert.True(role1 != role2);
    }

    /// <summary>The equality operator returns false when only one side is null.</summary>
    [Fact]
    public void OperatorEquals_OneSideNull_ReturnsFalse_Test()
    {
        TermIdentity term = new("isManager", []);
        TermIdentity? nullTerm = null;

        Assert.False(term == nullTerm);
        Assert.False(nullTerm == term);
        Assert.True(term != nullTerm);
        Assert.True(nullTerm != term);
    }

    /// <summary>Boxed Equals returns true for an equal predicate name and arguments.</summary>
    [Fact]
    public void Equals_BoxedEqualNameAndArguments_ReturnsTrue_Test()
    {
        TermIdentity first = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        object second = new TermIdentity(
            "hasRole",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]
        );

        Assert.True(first.Equals(second));
    }

    /// <summary>Boxed Equals returns false for a different predicate name.</summary>
    [Fact]
    public void Equals_BoxedDifferentPredicateName_ReturnsFalse_Test()
    {
        TermIdentity role = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        object isManager = new TermIdentity(
            "isManager",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]
        );

        Assert.False(role.Equals(isManager));
    }

    /// <summary>Boxed Equals returns false for a different argument value.</summary>
    [Fact]
    public void Equals_BoxedDifferentArgumentValue_ReturnsFalse_Test()
    {
        TermIdentity role1 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        object role2 = new TermIdentity(
            "hasRole",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Z"))]
        );

        Assert.False(role1.Equals(role2));
    }

    /// <summary>Boxed Equals returns false for an unrelated object.</summary>
    [Fact]
    public void Equals_UnrelatedObject_ReturnsFalse_Test()
    {
        TermIdentity term = new("isManager", []);

        Assert.False(term.Equals("isManager"));
    }
}
