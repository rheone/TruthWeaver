namespace TruthWeaver.Tests;

using TruthWeaver.Ast;

/// <summary>
/// The shared op-name lookup (<see cref="TreeFormatOpNames"/>) that the JSON and YAML tree printers
/// and parsers all consume instead of each maintaining its own copy of the operator ⇄ tree-format
/// string table.
/// </summary>
public sealed class TreeFormatOpNamesTests
{
    /// <summary>ToTreeFormat maps every canonical op name to its tree format string.</summary>
    [Theory]
    [InlineData("Not", "not")]
    [InlineData("And", "and")]
    [InlineData("Or", "or")]
    [InlineData("Xor", "xor")]
    [InlineData("Equivalent", "equivalent")]
    [InlineData("Implies", "implies")]
    [InlineData("Nand", "nand")]
    [InlineData("Nor", "nor")]
    [InlineData("Parity", "parity")]
    [InlineData("Any", "any")]
    [InlineData("All", "all")]
    [InlineData("None", "none")]
    [InlineData("Between", "between")]
    [InlineData("Coalesce", "coalesce")]
    [InlineData("If", "if")]
    [InlineData("IsTrue", "isTrue")]
    [InlineData("IsFalse", "isFalse")]
    [InlineData("IsUnknown", "isUnknown")]
    [InlineData("IsKnown", "isKnown")]
    [InlineData("ExactlyOne", "exactlyOne")]
    [InlineData("AtLeast", "atLeast")]
    [InlineData("AtMost", "atMost")]
    [InlineData("GreaterThan", "greaterThan")]
    [InlineData("LessThan", "lessThan")]
    [InlineData("Exactly", "exactly")]
    public void ToTreeFormat_EveryCanonicalName_MapsToTreeFormat_Test(string opName, string expected)
    {
        Assert.Equal(expected, TreeFormatOpNames.ToTreeFormat(opName));
    }

    /// <summary>ToTreeFormat throws for an unrecognized op name.</summary>
    [Fact]
    public void ToTreeFormat_UnrecognizedName_Throws_Test()
    {
        Assert.Throws<InvalidOperationException>(() => TreeFormatOpNames.ToTreeFormat("Bogus"));
    }

    /// <summary>TryFromTreeFormat resolves every tree format string back to its canonical op name.</summary>
    [Theory]
    [InlineData("not", "Not")]
    [InlineData("and", "And")]
    [InlineData("or", "Or")]
    [InlineData("xor", "Xor")]
    [InlineData("equivalent", "Equivalent")]
    [InlineData("xnor", "Equivalent")]
    [InlineData("iff", "Equivalent")]
    [InlineData("IFF", "Equivalent")]
    [InlineData("implies", "Implies")]
    [InlineData("nand", "Nand")]
    [InlineData("NOR", "Nor")]
    [InlineData("IMPLIES", "Implies")]
    [InlineData("parity", "Parity")]
    [InlineData("PARITY", "Parity")]
    [InlineData("any", "Any")]
    [InlineData("ALL", "All")]
    [InlineData("None", "None")]
    [InlineData("between", "Between")]
    [InlineData("COALESCE", "Coalesce")]
    [InlineData("IF", "If")]
    [InlineData("istrue", "IsTrue")]
    [InlineData("ISFALSE", "IsFalse")]
    [InlineData("isUnknown", "IsUnknown")]
    [InlineData("ISKNOWN", "IsKnown")]
    [InlineData("exactlyOne", "ExactlyOne")]
    [InlineData("atLeast", "AtLeast")]
    [InlineData("atMost", "AtMost")]
    [InlineData("greaterThan", "GreaterThan")]
    [InlineData("lessThan", "LessThan")]
    [InlineData("exactly", "Exactly")]
    public void TryFromTreeFormat_EveryString_ResolvesToCanonicalName_Test(string formatName, string expectedOpName)
    {
        bool resolved = TreeFormatOpNames.TryFromTreeFormat(formatName, out string? opName);

        Assert.True(resolved);
        Assert.Equal(expectedOpName, opName);
    }

    /// <summary>TryFromTreeFormat is case insensitive.</summary>
    [Theory]
    [InlineData("AND")]
    [InlineData("And")]
    [InlineData("aNd")]
    [InlineData("ATLEAST")]
    [InlineData("AtLeast")]
    public void TryFromTreeFormat_Input_IsCaseInsensitive_Test(string formatName)
    {
        bool resolved = TreeFormatOpNames.TryFromTreeFormat(formatName, out string? opName);

        Assert.True(resolved);
        Assert.NotNull(opName);
    }

    /// <summary>TryFromTreeFormat returns false for an unrecognized format string.</summary>
    [Fact]
    public void TryFromTreeFormat_UnrecognizedString_ReturnsFalse_Test()
    {
        bool resolved = TreeFormatOpNames.TryFromTreeFormat("bogus", out string? opName);

        Assert.False(resolved);
        Assert.Null(opName);
    }

    /// <summary>ReadableNames lists the quantifier op strings, so a quantifier typo gets a suggestion.</summary>
    [Theory]
    [InlineData("forall")]
    [InlineData("exists")]
    public void ReadableNames_QuantifierOpStrings_ArePresent_Test(string name)
    {
        Assert.Contains(name, TreeFormatOpNames.ReadableNames);
    }

    /// <summary>The quantifier op strings stay out of the operator lookup, because a quantifier has no canonical op-name entry.</summary>
    [Theory]
    [InlineData("forall")]
    [InlineData("exists")]
    public void TryFromTreeFormat_QuantifierOpString_ReturnsFalse_Test(string name)
    {
        Assert.False(TreeFormatOpNames.TryFromTreeFormat(name, out _));
    }
}
