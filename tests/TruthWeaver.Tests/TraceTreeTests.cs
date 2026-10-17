namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="Decision.TraceTree"/>: a structural mirror of the compiled tree, annotated per node
/// by what happened during evaluation, in lockstep with the flat <see cref="Trace"/> the same
/// evaluation produces.
/// </summary>
public sealed class TraceTreeTests
{
    /// <summary>Every node in a fully evaluated tree has a result.</summary>
    [Fact]
    public async Task Tree_FullyEvaluated_EveryNodeHasResult_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.NotNull(decision.TraceTree);
        TraceNode root = decision.TraceTree;
        Assert.Equal(TruthValue.True, root.Result);
        Assert.False(root.NotEvaluated);
        Assert.Equal(2, root.Children.Count);
        Assert.All(root.Children, child => Assert.Equal(TruthValue.True, child.Result));
    }

    /// <summary>An AND short circuit marks the skipped operand as not evaluated, with no children.</summary>
    [Fact]
    public async Task ShortCircuit_And_MarksSkippedOperandNotEvaluated_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        TraceNode root = decision.TraceTree!;
        Assert.Equal(TruthValue.False, root.Result);
        TraceNode skippedOperand = root.Children[1];
        Assert.True(skippedOperand.NotEvaluated);
        Assert.Null(skippedOperand.Result);
        Assert.Empty(skippedOperand.Children);
    }

    /// <summary>Skipping a whole subtree operand leaves it with no children, whatever its static shape.</summary>
    [Fact]
    public async Task Skipped_WholeSubtreeOperand_HasNoChildren_Test()
    {
        // "a AND (b OR c)" with a = false: the (b OR c) OrExpression is itself the second AND operand,
        // so the whole subtree is skipped as one unit - nothing about b or c is recorded.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND (b OR c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Empty(skippedSubtree.Children);
    }

    /// <summary>A skipped OR subtree is described as OR.</summary>
    [Fact]
    public async Task Skipped_OrSubtree_IsDescribedAsOr_Test()
    {
        // "a AND (b OR c)" with a = false short-circuits before the (b OR c) OrExpression is
        // evaluated, so its skipped TraceNode is labelled from the node's static shape alone.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND (b OR c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("OR", skippedSubtree.Text);
    }

    /// <summary>A skipped AND subtree is described as AND.</summary>
    [Fact]
    public async Task Skipped_AndSubtree_IsDescribedAsAnd_Test()
    {
        // "a OR (b AND c)" with a = true short-circuits before the (b AND c) AndExpression is
        // evaluated, so its skipped TraceNode is labelled from the node's static shape alone.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a OR (b AND c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("AND", skippedSubtree.Text);
    }

    /// <summary>A skipped NOT subtree is described as NOT.</summary>
    [Fact]
    public async Task Skipped_NotSubtree_IsDescribedAsNot_Test()
    {
        // "a AND (NOT b)" with a = false short-circuits before the (NOT b) NotExpression is
        // evaluated, so its skipped TraceNode is labelled from the node's static shape alone.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND (NOT b)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("NOT", skippedSubtree.Text);
    }

    /// <summary>A skipped XOR subtree is described as XOR.</summary>
    [Fact]
    public async Task Skipped_XorSubtree_IsDescribedAsXor_Test()
    {
        // "a AND (b XOR c)" with a = false short-circuits before the (b XOR c) XorExpression is
        // evaluated, so its skipped TraceNode is labelled from the node's static shape alone.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Xor(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c")))
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("XOR", skippedSubtree.Text);
    }

    [Fact]
    public async Task Describe_SkippedEquivalentSubtree_ReturnsEquivalent_Test()
    {
        // "a AND (b XNOR c)" with a = false short-circuits before the (b XNOR c) XnorExpression is
        // evaluated, so its skipped TraceNode is labelled from the node's static shape alone.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Xnor(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c")))
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("EQUIVALENT", skippedSubtree.Text);
    }

    /// <summary>A skipped ExactlyOne subtree is described as ExactlyOne.</summary>
    [Fact]
    public async Task Skipped_ExactlyOneSubtree_IsDescribedAsExactlyOne_Test()
    {
        // "a AND ExactlyOne(b, c, d)" with a = false short-circuits before the ExactlyOneExpression
        // is evaluated, so its skipped TraceNode is labelled from the node's static shape alone.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .AddConstant("d", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(
                RuleBuilder.Predicate("a"),
                RuleBuilder.ExactlyOne(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"), RuleBuilder.Predicate("d"))
            )
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("ExactlyOne", skippedSubtree.Text);
    }

    /// <summary>A skipped threshold subtree is described in the default OpName and K format.</summary>
    [Fact]
    public async Task Skipped_ThresholdSubtree_UsesOpNameKFormat_Test()
    {
        // "a AND AtLeast(2, b, c, d)" with a = false short-circuits before the ThresholdExpression is
        // evaluated, so its skipped TraceNode falls through to the default arm's
        // "OpName(K)" formatting rather than one of the named arms.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .AddConstant("d", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(
                RuleBuilder.Predicate("a"),
                RuleBuilder.AtLeast(2, RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"), RuleBuilder.Predicate("d"))
            )
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        TraceNode skippedSubtree = decision.TraceTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("AtLeast(2)", skippedSubtree.Text);
    }
}
