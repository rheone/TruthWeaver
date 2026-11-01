namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary><see cref="CompiledRule{TContext}.Outline"/>: every operator's and predicate's label/description, recursively.</summary>
public sealed class OperatorInfoTests
{
    /// <summary>Every operator node has a non empty label and description.</summary>
    [Theory]
    [InlineData("true", "True")]
    [InlineData("false", "False")]
    [InlineData("a AND b", "AND")]
    [InlineData("a OR b", "OR")]
    [InlineData("NOT a", "NOT")]
    [InlineData("(a XOR b)", "XOR")]
    [InlineData("(a EQUIVALENT b)", "EQUIVALENT")]
    [InlineData("(a IMPLIES b)", "IMPLIES")]
    [InlineData("(a NAND b)", "NAND")]
    [InlineData("(a NOR b)", "NOR")]
    [InlineData("PARITY(a, b)", "PARITY")]
    [InlineData("ANY(a, b)", "ANY")]
    [InlineData("ALL(a, b)", "ALL")]
    [InlineData("NONE(a, b)", "NONE")]
    [InlineData("BETWEEN(0, 1, a, b)", "BETWEEN(0, 1)")]
    [InlineData("COALESCE(a, b)", "COALESCE")]
    [InlineData("If(a, b, a)", "If")]
    [InlineData("IsTrue(a)", "IsTrue")]
    [InlineData("IsFalse(a)", "IsFalse")]
    [InlineData("IsUnknown(a)", "IsUnknown")]
    [InlineData("IsKnown(a)", "IsKnown")]
    [InlineData("ExactlyOne(a, b)", "ExactlyOne")]
    [InlineData("AtLeast(1, a, b)", "AtLeast(1)")]
    [InlineData("AtMost(1, a, b)", "AtMost(1)")]
    [InlineData("GreaterThan(0, a, b)", "GreaterThan(0)")]
    [InlineData("LessThan(2, a, b)", "LessThan(2)")]
    [InlineData("Exactly(1, a, b)", "Exactly(1)")]
    public void Describe_EveryOperatorNode_HasLabelAndDescription_Test(string dsl, string expectedLabel)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        OutlineNode description = Compile(compiler, dsl).Outline();

        Assert.Equal(expectedLabel, description.Label);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
    }

    /// <summary>A term node is described from its registered predicate schema.</summary>
    [Fact]
    public void Describe_TermNode_UsesRegisteredPredicateSchema_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        OutlineNode description = Compile(compiler, "hasRole(role: \"Y\")").Outline();

        Assert.Equal("hasRole", description.Label);
        Assert.Contains("hasRole", description.Description, StringComparison.Ordinal);
        Assert.Empty(description.Operands);
    }

    /// <summary>An operator node's operands are described recursively.</summary>
    [Fact]
    public void Describe_OperatorNode_DescribesOperandsRecursively_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        OutlineNode description = Compile(compiler, "a AND b").Outline();

        Assert.Equal("AND", description.Label);
        Assert.Equal(2, description.Operands.Count);
        Assert.Equal("a", description.Operands[0].Label);
        Assert.Equal("b", description.Operands[1].Label);
    }

    /// <summary>The registry resolves a predicate's label and description by name.</summary>
    [Fact]
    public void Lookup_Registry_ResolvesLabelAndDescriptionByName_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddStringArgPredicate("hasRole", "role", "Y")
            .Build();

        bool found = registry.TryGetSchema("hasRole", out PredicateSchema? schema);

        Assert.True(found);
        Assert.Equal("hasRole", schema!.Label);
        Assert.False(string.IsNullOrWhiteSpace(schema.Description));
    }

    /// <summary>Registry lookup is case insensitive and reports not found for an unregistered name.</summary>
    [Fact]
    public void Lookup_Registry_IsCaseInsensitiveAndReportsNotFound_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", true)
            .Build();

        Assert.True(registry.TryGetSchema("ISMANAGER", out PredicateSchema? found));
        Assert.NotNull(found);
        Assert.False(registry.TryGetSchema("neverRegistered", out PredicateSchema? notFound));
        Assert.Null(notFound);
    }

    /// <summary>A quantifier node is described with the canonical FORALL or EXISTS label and a real description.</summary>
    [Theory]
    [InlineData(QuantifierKind.ForAll, "FORALL")]
    [InlineData(QuantifierKind.Exists, "EXISTS")]
    public void Describe_QuantifierNode_ReturnsCanonicalLabelAndDescription_Test(QuantifierKind kind, string expectedLabel)
    {
        QuantifierExpression node = new(kind, "line", "lines", new ConstantExpression(TruthValue.True));

        OperatorDescriptor descriptor = OperatorInfo.Describe(node);

        Assert.Equal(expectedLabel, descriptor.Label);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Description));
    }

    /// <summary>Outline throws for a term expression and points callers at the predicate schema.</summary>
    [Fact]
    public void Outline_TermExpression_ThrowsPointingAtPredicateSchema_Test()
    {
        TermExpression term = new(new TermIdentity("isManager", []));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => OperatorInfo.Describe(term));

        Assert.Equal("node", exception.ParamName);
        Assert.Contains("PredicateSchema", exception.Message, StringComparison.Ordinal);
    }

    private static CompiledRule<RuleTestContext> Compile(RuleCompiler<RuleTestContext> compiler, string dsl)
    {
        CompilationResult<RuleTestContext> result = compiler.Compile(dsl);
        Assert.True(result.Succeeded);
        return result.CompiledRule!;
    }
}
