namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 02: human-readable rendering of a <see cref="RuleDiffResult"/>.</summary>
public sealed class RuleDiffPrinterTests
{
    /// <summary>A null diff throws ArgumentNullException naming the diff parameter.</summary>
    [Fact]
    public void Print_NullDiff_ThrowsNamingDiffParameter_Test()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => RuleDiffPrinter.Print(null!));

        Assert.Equal("diff", exception.ParamName);
    }

    /// <summary>An empty diff renders as no changes.</summary>
    [Fact]
    public void Print_EmptyDiff_RendersNoChanges_Test()
    {
        RuleDiffResult diff = new([]);

        string text = RuleDiffPrinter.Print(diff);

        Assert.Equal("No changes.", text);
    }

    /// <summary>An added operand renders using its label and description, not the AST type name.</summary>
    [Fact]
    public void Print_AddedOperand_RendersLabelNotAstTypeName_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        Assert.Contains("Added", text, StringComparison.Ordinal);
        Assert.Contains("hasRole", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TermExpression", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AndExpression", text, StringComparison.Ordinal);
    }

    /// <summary>A removed operand renders using its before label.</summary>
    [Fact]
    public void Print_RemovedOperand_RendersBeforeLabel_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        Assert.Contains("Removed", text, StringComparison.Ordinal);
        Assert.Contains("hasRole", text, StringComparison.Ordinal);
    }

    /// <summary>A changed operator renders both the before and after labels.</summary>
    [Fact]
    public void Print_ChangedOperator_RendersBeforeAndAfterLabels_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager OR isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        Assert.Contains("AND", text, StringComparison.Ordinal);
        Assert.Contains("OR", text, StringComparison.Ordinal);
    }

    /// <summary>Rendering produces one line per diff entry.</summary>
    [Fact]
    public void Print_Entries_OneLineEach_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND hasRole(role: \"Z\") AND isDepartmentHead")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        string[] lines = text.Split('\n');
        Assert.Equal(diff.Entries.Count, lines.Length);
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
