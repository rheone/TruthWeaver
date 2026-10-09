namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

public sealed class RuleAssertionsTests
{
    /// <summary>Two rules that differ only in operand order are equivalent, so the assertion passes.</summary>
    [Fact]
    public void AssertEquivalent_EquivalentRules_DoesNotThrow_Test()
    {
        RuleAssertions.AssertEquivalent(Compile("a AND b"), Compile("b AND a"));
    }

    /// <summary>A non-equivalent pair throws and the message shows a counter-example assignment.</summary>
    [Fact]
    public void AssertEquivalent_NotEquivalentRules_ThrowsWithCounterExample_Test()
    {
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RuleAssertions.AssertEquivalent(Compile("a AND b"), Compile("a OR b"))
        );

        Assert.Contains("not equivalent", exception.Message);
        Assert.Contains("a", exception.Message);
        Assert.Contains("b", exception.Message);
        Assert.Matches("(True|False|Unknown)", exception.Message);
    }

    /// <summary>An undecided comparison throws as inconclusive and names the term cap.</summary>
    [Fact]
    public void AssertEquivalent_TermCapExceeded_ThrowsInconclusiveNamingCap_Test()
    {
        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            RuleAssertions.AssertEquivalent(Compile("a AND b"), Compile("b AND a"), new CompilerOptions(MaxAnalysisTerms: 1))
        );

        Assert.Contains("inconclusive", exception.Message);
        Assert.Contains("MaxAnalysisTerms", exception.Message);
        Assert.Contains("Raise", exception.Message);
    }

    /// <summary>Raising the term cap through the options lets a larger comparison complete.</summary>
    [Fact]
    public void AssertEquivalent_RaisedTermCap_DecidesTheComparison_Test()
    {
        RuleAssertions.AssertEquivalent(Compile("a AND b"), Compile("b AND a"), new CompilerOptions(MaxAnalysisTerms: 2));
    }

    /// <summary>A null rule is a programming error and throws ArgumentNullException.</summary>
    [Fact]
    public void AssertEquivalent_NullRule_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() => RuleAssertions.AssertEquivalent(null!, Compile("a")));
    }

    private static CompiledRule<object?> Compile(string text)
    {
        PredicateRegistryBuilder<object?> builder = PredicateRegistry<object?>.CreateBuilder();
        foreach (string name in new[] { "a", "b", "c" })
        {
            (PredicateSchema schema, var evaluate) = FakePredicates.Returning<object?>(name, true);
            builder.Add(schema, evaluate);
        }

        return new RuleCompiler<object?>(builder.Build()).Compile(text).CompiledRule!;
    }
}
