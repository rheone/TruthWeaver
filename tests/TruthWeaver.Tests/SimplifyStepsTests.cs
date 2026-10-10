namespace TruthWeaver.Tests;

using TruthWeaver.Evaluation;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.SimplifyWithSteps</c> returns the same rule as <c>Simplify</c> and the laws it applied, in order.
/// </summary>
public sealed class SimplifyStepsTests
{
    /// <summary>An already-simple rule returns an empty step list and the same text.</summary>
    [Theory]
    [InlineData("a")]
    [InlineData("a AND b")]
    [InlineData("NOT a OR a")]
    [InlineData("NOT a OR b")]
    public void SimplifyWithSteps_AlreadySimpleRule_ReturnsNoSteps_Test(string ruleText)
    {
        CompiledRule<RuleTestContext> rule = K3Rule.TryCreate(ruleText, 2)!.Compiled;

        SimplifyResult<RuleTestContext> result = rule.SimplifyWithSteps();

        Assert.Empty(result.Steps);
        Assert.Equal(rule.Simplify().CanonicalText, result.Rule.CanonicalText);
    }

    /// <summary>Each step names its law and shows the changed subtree before and after.</summary>
    [Theory]
    [InlineData("a AND True", RewriteLaw.Identity, "True AND a", "a")]
    [InlineData("a AND False", RewriteLaw.Annihilator, "False AND a", "False")]
    [InlineData("NOT True", RewriteLaw.ConstantFold, "NOT True", "False")]
    [InlineData("a AND a", RewriteLaw.Idempotence, "a AND a", "a")]
    [InlineData("NOT NOT a", RewriteLaw.DoubleNegation, "NOT NOT a", "a")]
    [InlineData("a AND (b AND c)", RewriteLaw.Flatten, "a AND (b AND c)", "a AND b AND c")]
    [InlineData("a AND (a OR b)", RewriteLaw.Absorption, "a AND (a OR b)", "a")]
    [InlineData("NOT (NOT a AND NOT b)", RewriteLaw.DeMorgan, "NOT (NOT a AND NOT b)", "a OR b")]
    [InlineData("If(True, a, b)", RewriteLaw.If, "If(True, a, b)", "a")]
    [InlineData("ANY(a, b)", RewriteLaw.AliasCollapse, "ANY(a, b)", "a OR b")]
    [InlineData("b AND a", RewriteLaw.Reorder, "b AND a", "a AND b")]
    public void SimplifyWithSteps_SingleLaw_ReportsLawBeforeAndAfter_Test(
        string ruleText,
        RewriteLaw law,
        string before,
        string after
    )
    {
        CompiledRule<RuleTestContext> rule = K3Rule.TryCreate(ruleText, 3)!.Compiled;

        SimplifyResult<RuleTestContext> result = rule.SimplifyWithSteps();

        Assert.Contains(result.Steps, step => step.Law == law && step.Before == before && step.After == after);
    }

    /// <summary>Steps come in the order the rewrite applied them, and each one is a change.</summary>
    [Fact]
    public void SimplifyWithSteps_SeveralLaws_ListsThemInApplicationOrder_Test()
    {
        CompiledRule<RuleTestContext> rule = K3Rule.TryCreate("(a AND True) AND (a OR b)", 2)!.Compiled;

        SimplifyResult<RuleTestContext> result = rule.SimplifyWithSteps();

        RewriteLaw[] laws = [.. result.Steps.Select(s => s.Law)];
        Assert.True(Array.IndexOf(laws, RewriteLaw.Identity) < Array.IndexOf(laws, RewriteLaw.Absorption));
        Assert.All(result.Steps, step => Assert.NotEqual(step.Before, step.After));
    }

    /// <summary>
    /// For generated rules the rule in the result is the rule from Simplify, and the step list is empty exactly when
    /// Simplify leaves the rule as written.
    /// </summary>
    [Fact]
    public void SimplifyWithSteps_GeneratedRules_MatchSimplifyAndAreEmptyOnlyForUnchangedRules_Test()
    {
        Random random = new(20261004);
        List<string> failures = [];
        int changed = 0;
        for (int i = 0; i < 600; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3, ["a", "b", "c"]);
            if (K3Rule.TryCreate(generated.Text, 3) is not { } original)
            {
                continue;
            }

            SimplifyResult<RuleTestContext> result = original.Compiled.SimplifyWithSteps();
            string expected = original.Compiled.Simplify().CanonicalText;
            if (result.Rule.CanonicalText != expected)
            {
                failures.Add($"{generated.Text}: result {result.Rule.CanonicalText} but Simplify gives {expected}");
            }

            bool unchanged = expected == original.Compiled.CanonicalText;
            changed += unchanged ? 0 : 1;
            if (unchanged != (result.Steps.Count == 0))
            {
                failures.Add($"{generated.Text}: {result.Steps.Count} steps, unchanged={unchanged}");
            }
        }

        Assert.Empty(failures);
        Assert.True(changed > 100, $"only {changed} rules changed");
    }
}
