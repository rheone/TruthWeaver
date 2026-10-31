namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Rewriting;

/// <summary>
/// Ticket 06 (coverage-gaps-2026-10-25): <c>NandNorExpander.ConvertThreshold</c>'s vacuous-constant-threshold
/// shortcut, reached directly against the internal AST, since <c>RuleNodeCompiler.Build</c> already rejects a
/// vacuous threshold (one whose outcome operand count alone decides) as an <c>InvalidThresholdValue</c> compile
/// error, so no DSL, JSON, YAML or <c>RuleBuilder</c> text can put one into a successfully compiled tree.
/// </summary>
public sealed class NandNorExpanderTests
{
    /// <summary>
    /// <c>AtLeast(0, ...)</c> is vacuously true for any operand values; the conversion short-circuits straight to the
    /// constant rather than going through <c>ThresholdExpansion</c>.
    /// </summary>
    [Fact]
    public void ToNand_ThresholdVacuouslyConstantByOperandCountAlone_RewritesToTheBareConstant_Test()
    {
        ThresholdExpression threshold = new(
            ThresholdComparison.AtLeast,
            K: 0,
            new EquatableArray<Expression>([Term("a"), Term("b")])
        );

        Expression? result = NandNorExpander.ToNand(threshold, maxNodes: 1000);

        Assert.Equal(new ConstantExpression(TruthValue.True), result);
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
