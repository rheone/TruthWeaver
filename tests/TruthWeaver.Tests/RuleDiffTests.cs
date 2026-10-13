namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 01: structural diff between two compiled rules.</summary>
public sealed class RuleDiffTests
{
    /// <summary>Structurally identical rules produce an empty diff.</summary>
    [Fact]
    public void Diff_StructurallyIdenticalRules_ProducesEmptyDiff_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        Assert.False(diff.HasChanges);
        Assert.Empty(diff.Entries);
    }

    /// <summary>A changed operator produces a single changed entry at the root.</summary>
    [Fact]
    public void Diff_ChangedOperator_ProducesSingleChangedEntry_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager OR isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
        Assert.Equal("AND", entry.Before!.Label);
        Assert.Equal("OR", entry.After!.Label);
    }

    /// <summary>Changed term arguments produce a changed entry at the term position.</summary>
    [Fact]
    public void Diff_ChangedTermArguments_ProduceChangedEntry_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND hasRole(role: \"Z\")").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Equal([1], entry.Path);
        Assert.Equal("hasRole", entry.Before!.Label);
        Assert.Equal("hasRole", entry.After!.Label);
    }

    /// <summary>An added operand produces an added entry.</summary>
    [Fact]
    public void Diff_AddedOperand_ProducesAddedEntry_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Added, entry.Kind);
        Assert.Equal([2], entry.Path);
        Assert.Null(entry.Before);
        Assert.Equal("hasRole", entry.After!.Label);
    }

    /// <summary>A removed operand produces a removed entry.</summary>
    [Fact]
    public void Diff_RemovedOperand_ProducesRemovedEntry_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Removed, entry.Kind);
        Assert.Equal([2], entry.Path);
        Assert.Equal("hasRole", entry.Before!.Label);
        Assert.Null(entry.After);
    }

    /// <summary>A change nested inside an unchanged operator is reported at its own path.</summary>
    [Fact]
    public void Diff_NestedChange_ReportedAtItsOwnPath_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("isManager AND (isDepartmentHead OR hasRole(role: \"Y\"))")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND (isDepartmentHead OR hasRole(role: \"Z\"))")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Equal([1, 1], entry.Path);
    }

    /// <summary>A different inspection kind is a change of the node, not an identical rule.</summary>
    [Fact]
    public void Diff_ChangedInspectionKind_ProducesChangedEntryAtRoot_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("IsTrue(isManager)").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("IsFalse(isManager)").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
    }

    /// <summary>
    /// A pair over the default analysis term cap is undecided by default, and decided once the cap is raised
    /// through the optional options parameter.
    /// </summary>
    [Fact]
    public void Compare_RulesOverTheDefaultTermCap_AreDecidedOnlyWhenTheCapIsRaised_Test()
    {
        string[] terms = [.. Enumerable.Range(1, 21).Select(i => $"t{i}")];
        PredicateRegistryBuilder<RuleTestContext> registry = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (string term in terms)
        {
            registry.AddConstant(term, true);
        }

        RuleCompiler<RuleTestContext> compiler = new(registry.Build());
        CompiledRule<RuleTestContext> before = compiler.Compile(string.Join(" AND ", terms)).CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile(string.Join(" AND ", terms.Reverse())).CompiledRule!;

        RuleDiffResult byDefault = RuleDiff.Compare(before, after);
        RuleDiffResult raised = RuleDiff.Compare(before, after, 21);

        Assert.True(byDefault.HasChanges);
        Assert.Null(byDefault.PreservesMeaning);
        Assert.True(raised.PreservesMeaning);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
