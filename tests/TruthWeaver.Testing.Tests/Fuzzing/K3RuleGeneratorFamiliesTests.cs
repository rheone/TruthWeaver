namespace TruthWeaver.Testing.Tests.Fuzzing;

using System.Text.RegularExpressions;
using TruthWeaver.Testing.Fuzzing;

/// <summary>The operator families option of <see cref="K3RuleGenerator"/>.</summary>
public sealed class K3RuleGeneratorFamiliesTests
{
    private static readonly string[] Terms = ["a", "b", "c"];

    /// <summary>With every family, the rule of a seed is the same as with the overload that has no families.</summary>
    [Fact]
    public void GenerateRule_AllFamilies_MatchesTheOverloadWithoutFamilies_Test()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            string plain = K3RuleGenerator.GenerateRule(new Random(seed), 3, Terms).Text;
            string all = K3RuleGenerator.GenerateRule(new Random(seed), 3, Terms, RuleOperatorFamilies.All).Text;

            Assert.Equal(plain, all);
        }
    }

    /// <summary>A rule from the connectives family holds no cardinality, <c>If</c> or inspection operator.</summary>
    [Fact]
    public void GenerateRule_ConnectivesOnly_UsesNoOtherOperatorFamily_Test()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            string text = K3RuleGenerator.GenerateRule(new Random(seed), 3, Terms, RuleOperatorFamilies.Connectives).Text;

            Assert.DoesNotMatch(
                new Regex(
                    @"\b(ANY|ALL|NONE|BETWEEN|ExactlyOne|AtLeast|AtMost|Exactly|GreaterThan|LessThan|If|Is\w+|COALESCE)\(",
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1)
                ),
                text
            );
        }
    }

    /// <summary>No family gives a single term or constant.</summary>
    [Fact]
    public void GenerateRule_NoFamily_GivesALeaf_Test()
    {
        GeneratedRule rule = K3RuleGenerator.GenerateRule(new Random(1), 4, Terms, RuleOperatorFamilies.None);

        Assert.Empty(rule.Children);
    }
}
