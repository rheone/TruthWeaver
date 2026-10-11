namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>A host lists the schemas a registry holds and the predicate names a compiled rule references.</summary>
public sealed class EnumerationMembersTests
{
    private static readonly PredicateRegistry<RuleTestContext> Registry = PredicateRegistry<RuleTestContext>
        .CreateBuilder()
        .AddConstant("isAdmin", true)
        .AddConstant("hasLicense", true)
        .Build();

    /// <summary>Schemas lists every registered schema, in the casing it was registered with.</summary>
    [Fact]
    public void Schemas_AfterRegistration_ListsEveryRegisteredSchema_Test()
    {
        string[] names = [.. Registry.Schemas.Select(schema => schema.Name).Order(StringComparer.Ordinal)];

        Assert.Equal(["hasLicense", "isAdmin"], names);
    }

    /// <summary>A predicate used twice, and written in another casing, appears once in the registered casing.</summary>
    [Fact]
    public void PredicateNames_WhenARuleRepeatsAPredicate_ListsItOnceInRegisteredCasing_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("ISADMIN AND (isadmin OR hasLicense)");

        string[] names = [.. rule.PredicateNames.Order(StringComparer.Ordinal)];

        Assert.Equal(["hasLicense", "isAdmin"], names);
    }

    /// <summary>A rule with no terms references no predicate.</summary>
    [Fact]
    public void PredicateNames_WhenARuleHasNoTerms_IsEmpty_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True");

        Assert.Empty(rule.PredicateNames);
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        return new RuleCompiler<RuleTestContext>(Registry).Compile(text).CompiledRule!;
    }
}
