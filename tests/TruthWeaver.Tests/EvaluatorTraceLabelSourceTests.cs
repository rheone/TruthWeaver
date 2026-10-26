namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The evaluator must label an evaluated operator node from the operator table (<c>OperatorDefinitions</c>), the
/// same table a skipped sibling node already reads through <c>TraceText.Of</c>. A label change in the table has to
/// reach both an evaluated node and a skipped node identically.
/// </summary>
[Collection(OperatorLabelMutationCollection.Name)]
public sealed class EvaluatorTraceLabelSourceTests
{
    /// <summary>
    /// Changing PARITY's label in the operator table changes a fully evaluated PARITY node's trace text, exactly as
    /// it already changes a skipped PARITY node's text. Before the fix, the evaluated node kept the literal
    /// "PARITY" string regardless of the table.
    /// </summary>
    [Fact]
    public async Task EvaluatedParityNode_LabelChangedInOperatorTable_ShowsTheNewLabel_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        CompiledRule<RuleTestContext> rule = compiler.Compile("PARITY(a, b)").CompiledRule!;

        using (OperatorLabel.Swap("Parity", "Parity (relabelled)"))
        {
            Decision decision = await rule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

            TraceNode root = decision.TraceTree!;
            Assert.False(root.NotEvaluated);
            Assert.Equal("Parity (relabelled)", root.Text);
        }
    }

    /// <summary>
    /// The same label change reaches a skipped PARITY sibling (short-circuited by an earlier AND operand), so the
    /// evaluated and skipped texts for the same operator agree once both read the table.
    /// </summary>
    [Fact]
    public async Task EvaluatedAndSkippedParityNodes_LabelChangedInOperatorTable_AgreeOnTheNewLabel_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .AddConstant("c", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        // "a AND PARITY(b, c)" with a = false short-circuits before the PARITY(b, c) operand is evaluated, so its
        // skipped TraceNode is labelled from the node's static shape alone, like the other Skipped_*Subtree tests.
        CompiledRule<RuleTestContext> rule = compiler.Compile("a AND PARITY(b, c)").CompiledRule!;
        CompiledRule<RuleTestContext> evaluatedRule = compiler.Compile("PARITY(b, c)").CompiledRule!;

        using (OperatorLabel.Swap("Parity", "Parity (relabelled)"))
        {
            Decision skippedDecision = await rule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
            Decision evaluatedDecision = await evaluatedRule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

            TraceNode skippedParity = skippedDecision.TraceTree!.Children[1];
            TraceNode evaluatedParity = evaluatedDecision.TraceTree!;
            Assert.True(skippedParity.NotEvaluated);
            Assert.False(evaluatedParity.NotEvaluated);
            Assert.Equal("Parity (relabelled)", skippedParity.Text);
            Assert.Equal(skippedParity.Text, evaluatedParity.Text);
        }
    }
}
