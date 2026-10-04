namespace TruthWeaver.Tests;

using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 09: the opt-in lint rules that flag constructs the simplifier already knows are redundant.</summary>
public sealed class LintTests
{
    /// <summary>
    /// <c>IsKnown</c> of an operand that can never be <c>Unknown</c> (here an <c>IsTrue</c> inspection, which always
    /// yields <c>True</c> or <c>False</c>) is <c>True</c> for every input, so with the lint enabled it is reported as an
    /// informational finding that suggests replacing it with <c>True</c>.
    /// </summary>
    [Fact]
    public void Compile_IsKnownOverNeverUnknownOperand_ReportsRedundantInspectionWithConstantSuggestion_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("IsKnown(IsTrue(a))", LintRules.RedundantInspection);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.RedundantInspection);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal(DiagnosticSuggestionKind.Replacement, diagnostic.Suggestion?.Kind);
        Assert.Equal("True", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// A rule that would trigger every lint produces no lint finding at all under the default options, so existing rules
    /// do not start reporting new diagnostics unless a lint is switched on.
    /// </summary>
    [Fact]
    public void Compile_RuleWithRedundantInspection_WithDefaultOptions_ReportsNoLintFindings_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("IsKnown(IsTrue(a))", LintRules.None);

        Assert.DoesNotContain(result.Diagnostics, d => IsLint(d));
    }

    /// <summary>
    /// A <c>COALESCE</c> whose first operand can never be <c>Unknown</c> never reaches its later operands, so with the
    /// lint enabled it is reported with a suggestion to keep only the first operand.
    /// </summary>
    [Fact]
    public void Compile_CoalesceWithNeverUnknownFirstOperand_ReportsRedundantCoalesceKeepingFirstOperand_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("IsKnown(a) ?? b", LintRules.RedundantCoalesce);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.RedundantCoalesce);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("IsKnown(a)", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// An <c>If</c> whose condition can only ever be <c>True</c> always takes its first branch, so with the lint enabled it
    /// is reported with a suggestion to keep just that branch.
    /// </summary>
    [Fact]
    public void Compile_IfWithAlwaysTrueCondition_ReportsConstantConditionKeepingTrueBranch_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("If(IsKnown(IsTrue(a)), a, b)", LintRules.ConstantIfCondition);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.ConstantIfCondition);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("a", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// An <c>If</c> whose two branches are the same expression yields that expression whatever the condition is (even
    /// <c>Unknown</c>, by the consensus term), so with the lint enabled it is reported with that branch as the suggestion.
    /// </summary>
    [Fact]
    public void Compile_IfWithIdenticalBranches_ReportsIdenticalBranchesKeepingOneBranch_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("If(a, b, b)", LintRules.IdenticalIfBranches);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.IdenticalIfBranches);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("b", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// A threshold whose constant operands already settle it (here one operand is <c>True</c>, so <c>AtLeast(1, ...)</c>
    /// holds whatever <c>a</c> is) is reported with the constant it always equals as the suggestion.
    /// </summary>
    [Fact]
    public void Compile_ThresholdSettledByConstantOperand_ReportsVacuousCardinalityWithConstantSuggestion_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("AtLeast(1, a, TRUE)", LintRules.VacuousCardinality);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.VacuousCardinality);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("True", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// A repeated operand of an idempotent connective (<c>a AND b AND a</c>) changes nothing, so with the lint enabled the
    /// finding suggests the same connective with each distinct operand kept once, in first-seen order.
    /// </summary>
    [Fact]
    public void Compile_AndWithRepeatedOperand_ReportsDuplicateOperandsSuggestingDistinctOperands_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("a AND b AND a", LintRules.DuplicateOperands);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DuplicateOperands);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("a AND b", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// <c>NOT (NOT x)</c> is <c>x</c> in Strong K3 (negation swaps <c>True</c> and <c>False</c> and fixes <c>Unknown</c>, so
    /// two of them cancel), so with the lint enabled the finding suggests the inner operand.
    /// </summary>
    [Fact]
    public void Compile_DoubleNegation_ReportsDoubleNegationSuggestingInnerOperand_Test()
    {
        CompilationResult<RuleTestContext> result = Compile("NOT NOT a", LintRules.DoubleNegation);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DoubleNegation);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("a", diagnostic.Suggestion?.Text);
    }

    /// <summary>
    /// The oracle check: for each flagged rule the suggested replacement is compiled and compared with the original using
    /// the exact Strong K3 equivalence check, so a lint can never recommend a change that alters the rule's meaning
    /// (including for <c>Unknown</c> inputs).
    /// </summary>
    [Theory]
    [InlineData("IsKnown(IsTrue(a))")]
    [InlineData("IsUnknown(IsTrue(a))")]
    [InlineData("IsTrue(IsKnown(a))")]
    [InlineData("IsFalse(IsKnown(a))")]
    [InlineData("IsKnown(a) ?? b")]
    [InlineData("b ?? IsKnown(a) ?? c")]
    [InlineData("If(IsKnown(IsTrue(a)), a, b)")]
    [InlineData("If(IsUnknown(IsTrue(a)), a, b)")]
    [InlineData("If(a, b, b)")]
    [InlineData("AtLeast(1, a, TRUE)")]
    [InlineData("AtLeast(3, a, b, FALSE)")]
    [InlineData("BETWEEN(1, 2, a, TRUE, FALSE)")]
    [InlineData("a AND b AND a")]
    [InlineData("a OR a")]
    [InlineData("ANY(a, b, a)")]
    [InlineData("ALL(a, a, b)")]
    [InlineData("a ?? b ?? a")]
    [InlineData("NOT NOT a")]
    public void Compile_FlaggedRule_SuggestedReplacementIsK3Equivalent_Test(string rule)
    {
        CompilationResult<RuleTestContext> original = Compile(rule, LintRules.All);

        // Findings come outermost first, and the outermost one here always covers the whole rule.
        Diagnostic finding = original.Diagnostics.First(d => IsLint(d) && d.Suggestion is not null);

        CompilationResult<RuleTestContext> replacement = Compile(finding.Suggestion!.Text, LintRules.None);

        Assert.True(replacement.Succeeded, finding.Suggestion.Text);
        RuleEquivalenceResult verdict = RuleEquivalence.Compare(original.CompiledRule!, replacement.CompiledRule!);
        Assert.Equal(RuleEquivalenceOutcome.Equivalent, verdict.Outcome);
    }

    /// <summary>
    /// Constructs that look redundant in two-valued logic but are not under Strong K3 (<c>a XOR a</c> is <c>Unknown</c> when
    /// <c>a</c> is, and a repeated <c>PARITY</c> operand changes the parity) are never flagged by any lint.
    /// </summary>
    [Theory]
    [InlineData("a XOR a")]
    [InlineData("PARITY(a, a, b)")]
    [InlineData("a AND NOT a")]
    [InlineData("IsKnown(a)")]
    [InlineData("If(a, b, c)")]
    public void Compile_RuleRedundantOnlyInTwoValuedLogic_ReportsNoLintFindings_Test(string rule)
    {
        CompilationResult<RuleTestContext> result = Compile(rule, LintRules.All);

        Assert.DoesNotContain(result.Diagnostics, d => IsLint(d));
    }

    private static bool IsLint(Diagnostic diagnostic)
    {
        return string.CompareOrdinal(diagnostic.Code, "TRE0017") >= 0
            && string.CompareOrdinal(diagnostic.Code, "TRE0023") <= 0
            && diagnostic.Severity == DiagnosticSeverity.Info;
    }

    private static CompilationResult<RuleTestContext> Compile(string rule, LintRules lints)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build(),
            new CompilerOptions(Lints: lints)
        );
        return compiler.Compile(rule);
    }
}
