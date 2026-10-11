namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>A host finds the registered predicates that none of its compiled rules reference.</summary>
public sealed class PredicateAuditTests
{
    private static readonly PredicateRegistry<RuleTestContext> Registry = PredicateRegistry<RuleTestContext>
        .CreateBuilder()
        .AddConstant("isAdmin", true)
        .AddConstant("hasLicense", true)
        .AddConstant("isBanned", false)
        .Build();

    /// <summary>Predicates referenced by no rule are returned; referenced ones are not.</summary>
    [Fact]
    public void FindUnused_WhenSomePredicatesAreReferenced_ReturnsOnlyTheUnreferencedSchemas_Test()
    {
        CompiledRule<RuleTestContext>[] rules = [Compile("isAdmin"), Compile("isAdmin AND hasLicense")];

        string[] names = Names(PredicateAudit.FindUnused(Registry, rules));

        Assert.Equal(["isBanned"], names);
    }

    /// <summary>A rule that writes a predicate in another casing still counts as using it.</summary>
    [Fact]
    public void FindUnused_WhenARuleUsesADifferentCasing_TreatsThePredicateAsUsed_Test()
    {
        CompiledRule<RuleTestContext>[] rules = [Compile("ISADMIN OR hasLicense OR ISBANNED")];

        Assert.Empty(PredicateAudit.FindUnused(Registry, rules));
    }

    /// <summary>With no rules, every registered schema is unused.</summary>
    [Fact]
    public void FindUnused_WhenThereAreNoRules_ReturnsEverySchema_Test()
    {
        string[] names = Names(PredicateAudit.FindUnused(Registry, []));

        Assert.Equal(["hasLicense", "isAdmin", "isBanned"], names);
    }

    /// <summary>A registry with no predicates has nothing to report.</summary>
    [Fact]
    public void FindUnused_WhenTheRegistryIsEmpty_ReturnsNothing_Test()
    {
        PredicateRegistry<RuleTestContext> empty = PredicateRegistry<RuleTestContext>.CreateBuilder().Build();

        Assert.Empty(PredicateAudit.FindUnused(empty, [Compile("True")]));
    }

    private static string[] Names(IEnumerable<PredicateSchema> schemas)
    {
        return [.. schemas.Select(schema => schema.Name).Order(StringComparer.Ordinal)];
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        return new RuleCompiler<RuleTestContext>(Registry).Compile(text).CompiledRule!;
    }
}
