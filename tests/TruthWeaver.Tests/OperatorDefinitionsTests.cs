namespace TruthWeaver.Tests;

using TruthWeaver.Ast;

/// <summary>
/// The operator definition table (<see cref="OperatorDefinitions"/>): one descriptive definition per operator in the
/// closed set, which <see cref="OperatorInfo"/> and <see cref="TreeFormatOpNames"/> read from.
/// </summary>
public sealed class OperatorDefinitionsTests
{
    /// <summary>The closed operator set as independent literals, so a missing or extra definition fails here.</summary>
    private static readonly string[] ClosedSet =
    [
        "Not",
        "And",
        "Or",
        "Xor",
        "Equivalent",
        "Implies",
        "Nand",
        "Nor",
        "Parity",
        "Any",
        "All",
        "None",
        "ExactlyOne",
        "AtLeast",
        "AtMost",
        "GreaterThan",
        "LessThan",
        "Exactly",
        "Between",
        "Coalesce",
        "If",
        "IsTrue",
        "IsFalse",
        "IsUnknown",
        "IsKnown",
    ];

    /// <summary>Verifies the table holds exactly one definition for every operator in the closed set.</summary>
    [Fact]
    public void All_has_exactly_one_definition_per_operator_in_the_closed_set_Test()
    {
        string[] names = [.. OperatorDefinitions.All.Select(d => d.OpName)];

        Assert.Equal(ClosedSet.Order(StringComparer.Ordinal), names.Order(StringComparer.Ordinal));
    }

    /// <summary>Verifies every definition carries a value for every descriptive field.</summary>
    [Fact]
    public void All_every_definition_has_every_field_Test()
    {
        foreach (OperatorDefinition d in OperatorDefinitions.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(d.OpName));
            Assert.False(string.IsNullOrWhiteSpace(d.TreeFormatName));
            Assert.False(string.IsNullOrWhiteSpace(d.LabelTemplate));
            Assert.False(string.IsNullOrWhiteSpace(d.DescriptionTemplate));
            Assert.True(d.MinOperands >= 1, d.OpName);
            Assert.True(d.MaxOperands is null || d.MaxOperands >= d.MinOperands, d.OpName);
        }
    }

    /// <summary>Verifies canonical names and tree-format names are each unique (tree-format names ignoring case).</summary>
    [Fact]
    public void All_canonical_and_tree_format_names_are_unique_Test()
    {
        Assert.Equal(
            OperatorDefinitions.All.Count,
            OperatorDefinitions.All.Select(d => d.OpName).Distinct(StringComparer.Ordinal).Count()
        );
        Assert.Equal(
            OperatorDefinitions.All.Count,
            OperatorDefinitions.All.Select(d => d.TreeFormatName).Distinct(StringComparer.OrdinalIgnoreCase).Count()
        );
    }

    /// <summary>Verifies arity facts for unary, binary, ternary and n-ary operators.</summary>
    [Theory]
    [InlineData("Not", 1, 1)]
    [InlineData("IsKnown", 1, 1)]
    [InlineData("Xor", 2, 2)]
    [InlineData("Implies", 2, 2)]
    [InlineData("If", 3, 3)]
    [InlineData("And", 2, null)]
    [InlineData("Between", 2, null)]
    public void TryGet_reports_the_operand_arity_Test(string opName, int min, int? max)
    {
        Assert.True(OperatorDefinitions.TryGet(opName, out OperatorDefinition? d));
        Assert.Equal(min, d.MinOperands);
        Assert.Equal(max, d.MaxOperands);
    }

    /// <summary>Verifies a parameterised label substitutes the node's K and Max.</summary>
    [Fact]
    public void Label_substitutes_k_and_max_Test()
    {
        Assert.True(OperatorDefinitions.TryGet("Between", out OperatorDefinition? d));

        Assert.Equal("BETWEEN(1, 2)", d.Label(new NodeShape("Between", 1, [], 2)));
    }

    /// <summary>Verifies an unknown canonical name is not found.</summary>
    [Fact]
    public void TryGet_returns_false_for_an_unknown_op_name_Test()
    {
        Assert.False(OperatorDefinitions.TryGet("Bogus", out OperatorDefinition? d));
        Assert.Null(d);
    }
}
