namespace TruthWeaver.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Ast;
using TruthWeaver.Testing.Fuzzing;

/// <summary>
/// The rewrite, analyzer, equivalence and truth-table tests draw their rules from <see cref="K3RuleGenerator"/>. This
/// guard fails when an operator in <see cref="OperatorDefinitions"/> is added without the generator producing it, so a
/// new operator cannot reach those layers untested.
/// </summary>
public sealed class OperatorCoverageGuardTests
{
    // The same draw as the universal gate tests: depth 2 over three terms, many rules.
    private const int Rules = 2500;

    /// <summary>Over a large draw of generated rules, every operator of the closed set appears at least once.</summary>
    [Fact]
    public void GenerateRule_AcrossManyRules_UsesEveryOperatorDefinition_Test()
    {
        Random random = new(20261021);
        string[] texts =
        [
            .. Enumerable.Range(0, Rules).Select(_ => K3RuleGenerator.GenerateRule(random, 2, ["a", "b", "c"]).Text),
        ];

        string[] missing =
        [
            .. OperatorDefinitions
                .All.Select(definition => definition.OpName)
                .Where(opName =>
                {
                    // A whole word, so that Exactly does not match ExactlyOne; the rule text is case-insensitive.
                    Regex word = new($@"\b{Regex.Escape(opName)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    return !texts.Any(text => word.IsMatch(text));
                }),
        ];

        Assert.Empty(missing);
    }
}
