namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

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

    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
    );

    /// <summary>The connectives the DSL reads by symbol, as independent literals.</summary>
    private static readonly string[] SymbolConnectives =
    [
        "Not",
        "And",
        "Or",
        "Xor",
        "Equivalent",
        "Implies",
        "Nand",
        "Nor",
        "Coalesce",
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

    /// <summary>Verifies exactly the symbol connectives carry lexer-read symbols, and each has a symbolic spelling the lexer reads back.</summary>
    [Fact]
    public void All_every_symbol_connective_has_a_symbolic_spelling_the_parser_reads_Test()
    {
        string[] withSymbols = [.. OperatorDefinitions.All.Where(d => d.ReadSymbols is { Count: > 0 }).Select(d => d.OpName)];

        Assert.Equal(SymbolConnectives.Order(StringComparer.Ordinal), withSymbols.Order(StringComparer.Ordinal));
        foreach (OperatorDefinition d in OperatorDefinitions.All.Where(d => d.ReadSymbols is not null))
        {
            Assert.NotNull(d.SymbolicSpelling);
            Assert.Contains(d.SymbolicSpelling, d.ReadSymbols!);
            foreach (string symbol in d.ReadSymbols!)
            {
                Assert.True(OperatorDefinitions.TryGetBySymbol(symbol, out OperatorDefinition? found), symbol);
                Assert.Equal(d.OpName, found.OpName);
            }
        }
    }

    /// <summary>Verifies every symbol connective's symbolic spelling compiles to the same tree as its canonical word.</summary>
    [Fact]
    public void Compile_every_symbolic_spelling_builds_the_same_tree_as_the_word_Test()
    {
        foreach (OperatorDefinition d in OperatorDefinitions.All.Where(d => d.ReadSymbols is not null))
        {
            string symbolic = d.OpName == "Not" ? $"{d.SymbolicSpelling}a" : $"a {d.SymbolicSpelling} b";
            string named = d.OpName switch
            {
                "Not" => $"{d.LabelTemplate} a",
                "Coalesce" => "COALESCE(a, b)",
                _ => $"a {d.LabelTemplate} b",
            };

            string? fromSymbol = Compiler.Compile(symbolic).CompiledRule?.CanonicalText;
            string? fromWord = Compiler.Compile(named).CompiledRule?.CanonicalText;

            Assert.NotNull(fromWord);
            Assert.Equal(fromWord, fromSymbol);
        }
    }

    /// <summary>Verifies the quantifier spellings come from one place and are distinct per kind.</summary>
    [Fact]
    public void QuantifierText_every_kind_has_a_distinct_label_and_glyph_Test()
    {
        QuantifierKind[] kinds = Enum.GetValues<QuantifierKind>();

        Assert.Equal(kinds.Length, kinds.Select(QuantifierText.Label).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(kinds.Length, kinds.Select(QuantifierText.Glyph).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("FORALL", QuantifierText.Label(QuantifierKind.ForAll));
        Assert.Equal(QuantifierText.Glyph(QuantifierKind.Exists), QuantifierText.Glyph("EXISTS"));
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

    /// <summary>
    /// Verifies the threshold family records a one-operand minimum, matching what the compiler accepts, while the
    /// other n-ary operators keep their minimum of two.
    /// </summary>
    [Theory]
    [InlineData("AtLeast", 1)]
    [InlineData("AtMost", 1)]
    [InlineData("Exactly", 1)]
    [InlineData("GreaterThan", 1)]
    [InlineData("LessThan", 1)]
    [InlineData("Or", 2)]
    [InlineData("Parity", 2)]
    [InlineData("Any", 2)]
    [InlineData("ExactlyOne", 2)]
    [InlineData("Coalesce", 2)]
    public void TryGet_threshold_family_minimum_is_one_and_others_unchanged_Test(string opName, int min)
    {
        Assert.True(OperatorDefinitions.TryGet(opName, out OperatorDefinition? d));
        Assert.Equal(min, d.MinOperands);
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
