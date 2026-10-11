namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>ToNnf</c>, <c>ToCnf</c> and <c>ToDnf</c> rewrite a rule into negation, conjunctive and disjunctive normal form using
/// only Strong Kleene laws (De Morgan, double negation, distribution). <c>COALESCE</c>, the inspections and <c>If</c> are
/// atoms, and a threshold is an atom unless <see cref="NormalFormOptions.ExpandThresholds"/> is set.
/// </summary>
public sealed class NormalFormTests
{
    private const string WideThreshold = "AtLeast(2, a, b, c, d)";

    /// <summary>NOT is pushed to the terms by De Morgan and a double negation is removed.</summary>
    [Theory]
    [InlineData("NOT (a AND b)", "NOT a OR NOT b")]
    [InlineData("NOT (a OR b)", "NOT a AND NOT b")]
    [InlineData("NOT NOT a", "a")]
    [InlineData("NOT (a AND NOT (b OR c))", "NOT a OR (b OR c)")]
    public void ToNnf_NegatedConnectives_PushesNotToTheTerms_Test(string text, string expected)
    {
        Assert.Equal(expected, Nnf(text).CanonicalText);
    }

    /// <summary>Derived operators expand first, so the result has no derived operator left.</summary>
    [Theory]
    [InlineData("a IMPLIES b", "NOT a OR b")]
    [InlineData("a NAND b", "NOT a OR NOT b")]
    [InlineData("a NOR b", "NOT a AND NOT b")]
    [InlineData("NOT (a IMPLIES b)", "a AND NOT b")]
    public void ToNnf_DerivedOperators_ExpandBeforeNegationIsPushed_Test(string text, string expected)
    {
        Assert.Equal(expected, Nnf(text).CanonicalText);
    }

    /// <summary>COALESCE, an inspection and If are atoms: the NOT stays above them and their operands are normalized.</summary>
    [Theory]
    [InlineData("NOT COALESCE(a, True)", "NOT COALESCE(a, True)")]
    [InlineData("NOT IsTrue(a)", "NOT IsTrue(a)")]
    [InlineData("NOT If(a, b, c)", "NOT If(a, b, c)")]
    [InlineData("NOT IsKnown(NOT (a AND b))", "NOT IsKnown(NOT a OR NOT b)")]
    public void ToNnf_OpaqueOperators_AreAtomsAndKeepTheirNot_Test(string text, string expected)
    {
        Assert.Equal(expected, Nnf(text).CanonicalText);
    }

    /// <summary>No classical complement law is used: a contradiction and an excluded middle stay as written.</summary>
    [Theory]
    [InlineData("a AND NOT a")]
    [InlineData("a OR NOT a")]
    public void ToDnf_ComplementPairs_AreNotFolded_Test(string text)
    {
        Assert.Equal(text, Dnf(text).CanonicalText);
    }

    /// <summary>CNF distributes OR over AND; DNF distributes AND over OR.</summary>
    [Fact]
    public void ToCnfAndToDnf_MixedRule_DistributeInOppositeDirections_Test()
    {
        K3Rule rule = Rule("(a AND b) OR c", 3);

        Assert.Equal("(a OR c) AND (b OR c)", rule.Compiled.ToCnf().CompiledRule!.CanonicalText);
        Assert.Equal("(a AND b) OR c", rule.Compiled.ToDnf().CompiledRule!.CanonicalText);
    }

    /// <summary>The documented example: one negated rule in each of the three forms.</summary>
    [Fact]
    public void NormalForms_NegatedNestedRule_GiveTheDocumentedTexts_Test()
    {
        CompiledRule<RuleTestContext> rule = Rule("NOT (a AND (b OR c))", 3).Compiled;

        Assert.Equal("NOT a OR (NOT b AND NOT c)", rule.ToNnf().CompiledRule!.CanonicalText);
        Assert.Equal("NOT a OR (NOT b AND NOT c)", rule.ToDnf().CompiledRule!.CanonicalText);
        Assert.Equal("(NOT a OR NOT b) AND (NOT a OR NOT c)", rule.ToCnf().CompiledRule!.CanonicalText);
    }

    /// <summary>Every form evaluates like the original rule and the oracle for every assignment of generated rules.</summary>
    [Theory]
    [InlineData("nnf", 20262001)]
    [InlineData("cnf", 20262002)]
    [InlineData("dnf", 20262003)]
    public async Task NormalForm_GeneratedRules_EvaluateEqualToOriginalForEveryAssignment_Test(string form, int seed)
    {
        Random random = new(seed);
        List<string> failures = [];
        int checkedRules = 0;
        for (int i = 0; i < 700; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3, ["a", "b", "c"]);
            K3Rule? original = K3Rule.TryCreate(generated.Text, 3);
            CompiledRule<RuleTestContext>? rewritten = original is null
                ? null
                : Convert(original.Compiled, form, expand: i % 2 == 0);
            if (original is null || rewritten is null)
            {
                continue;
            }

            checkedRules++;
            K3Rule result = original.Rewrite(_ => rewritten);
            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision after = await result.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                if (after.Result != generated.Eval(assignment))
                {
                    failures.Add($"{generated.Text} @ [{string.Join(",", assignment)}]: {form} gives {after.Result}");
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(checkedRules > 300, $"only {checkedRules} rules were checked");
    }

    /// <summary>AssertSound accepts each form on generated rules and each form is idempotent.</summary>
    [Theory]
    [InlineData("nnf", 20262011)]
    [InlineData("cnf", 20262012)]
    [InlineData("dnf", 20262013)]
    public void NormalForm_GeneratedRules_PassAssertSoundWithIdempotence_Test(string form, int seed)
    {
        Random random = new(seed);
        int checkedRules = 0;
        for (int i = 0; i < 300; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3, ["a", "b", "c"]);
            if (K3Rule.TryCreate(generated.Text, 3)?.Compiled is not { } rule || Convert(rule, form, expand: false) is null)
            {
                continue;
            }

            checkedRules++;
            RewriteAssertions.AssertSound(rule, r => Convert(r, form, expand: false)!, RewriteExpectations.Idempotent);
        }

        Assert.True(checkedRules > 100, $"only {checkedRules} rules were checked");
    }

    /// <summary>With ExpandThresholds on, a threshold becomes AND/OR/NOT and the rule keeps its value.</summary>
    [Theory]
    [InlineData("AtLeast(2, a, b, c)")]
    [InlineData("AtMost(1, a, b, c)")]
    [InlineData("Exactly(1, a, b, c)")]
    [InlineData("NOT Exactly(2, a, b, c)")]
    [InlineData("BETWEEN(1, 2, a, b, c)")]
    [InlineData("PARITY(a, b, c)")]
    public void ToNnf_ExpandThresholdsOn_RemovesEveryThresholdAndKeepsTheValue_Test(string text)
    {
        CompiledRule<RuleTestContext> rule = Rule(text, 3).Compiled;
        NormalFormOptions on = new(ExpandThresholds: true);

        CompilationResult<RuleTestContext> result = rule.ToNnf(on);

        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("AtLeast", result.CompiledRule!.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("AtMost", result.CompiledRule.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("Exactly", result.CompiledRule.CanonicalText, StringComparison.Ordinal);
        RewriteAssertions.AssertSound(rule, r => r.ToNnf(on).CompiledRule!, RewriteExpectations.Idempotent);
        RewriteAssertions.AssertSound(rule, r => r.ToCnf(on).CompiledRule!, RewriteExpectations.Idempotent);
        RewriteAssertions.AssertSound(rule, r => r.ToDnf(on).CompiledRule!, RewriteExpectations.Idempotent);
    }

    /// <summary>With ExpandThresholds off, a wide threshold stays an atom and one TRE0031 warning gives the growth estimate.</summary>
    [Fact]
    public void ToNnf_ExpandThresholdsOff_KeepsThresholdAsAtomWithGrowthWarning_Test()
    {
        CompiledRule<RuleTestContext> rule = Rule($"a AND {WideThreshold}", 4).Compiled;

        CompilationResult<RuleTestContext> result = rule.ToNnf();

        Assert.Equal(rule.CanonicalText, result.CompiledRule!.CanonicalText);
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.ThresholdKeptAsAtom, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains(WideThreshold, warning.Message, StringComparison.Ordinal);
        Assert.Contains("19 nodes", warning.Message, StringComparison.Ordinal);
    }

    /// <summary>A threshold that is just an OR or an AND expands without the option, so it raises no warning.</summary>
    [Fact]
    public void ToNnf_ThresholdThatIsAnOrOrAnAnd_ExpandsWithoutWarning_Test()
    {
        CompilationResult<RuleTestContext> result = Rule("NOT (AtLeast(1, a, b) AND AtLeast(3, a, b, c))", 3).Compiled.ToNnf();

        Assert.Empty(result.Diagnostics);
        Assert.Equal("(NOT a AND NOT b) OR (NOT a OR NOT b OR NOT c)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A negated kept threshold keeps its NOT, because a threshold is an atom.</summary>
    [Fact]
    public void ToNnf_NegatedKeptThreshold_KeepsTheNotAbove_Test()
    {
        CompilationResult<RuleTestContext> result = Rule($"NOT {WideThreshold}", 4).Compiled.ToNnf();

        Assert.Equal($"NOT {WideThreshold}", result.CompiledRule!.CanonicalText);
        Assert.Single(result.Diagnostics);
    }

    /// <summary>A rule over the node cap returns TRE0016 and no rule, for each form.</summary>
    [Theory]
    [InlineData("nnf")]
    [InlineData("cnf")]
    [InlineData("dnf")]
    public void NormalForm_ResultOverTheCap_ReturnsRewriteTooLargeAndNoRule_Test(string form)
    {
        CompiledRule<RuleTestContext> rule = Rule("(a OR b) AND (c OR d) AND (e OR f)", 6).Compiled;
        const int tight = 5;

        CompilationResult<RuleTestContext> result = form switch
        {
            "nnf" => rule.ToNnf(maxNodeCount: tight),
            "cnf" => rule.ToCnf(maxNodeCount: tight),
            _ => rule.ToDnf(maxNodeCount: tight),
        };

        Assert.Null(result.CompiledRule);
        Assert.Equal(DiagnosticCodes.RewriteTooLarge, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A wide expanded threshold is refused at the default cap before its subsets are built.</summary>
    [Fact]
    public void ToDnf_WideThresholdExpanded_IsRefusedAtTheDefaultCap_Test()
    {
        // C(20, 10) = 184756 subsets is far beyond the default cap.
        CompiledRule<RuleTestContext> rule = Rule(
            "AtLeast(10, a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t)",
            20
        ).Compiled;

        CompilationResult<RuleTestContext> result = rule.ToDnf(new NormalFormOptions(ExpandThresholds: true));

        Assert.Null(result.CompiledRule);
        Assert.Equal(DiagnosticCodes.RewriteTooLarge, Assert.Single(result.Diagnostics).Code);
    }

    /// <summary>A rule that is already in a form comes back with the same text.</summary>
    [Theory]
    [InlineData("(a AND b) OR (NOT a AND c)", "dnf")]
    [InlineData("(a OR b) AND (NOT a OR c)", "cnf")]
    public void NormalForm_RuleAlreadyInTheForm_IsUnchanged_Test(string text, string form)
    {
        CompiledRule<RuleTestContext> rule = Rule(text, 3).Compiled;

        Assert.Equal(rule.CanonicalText, Convert(rule, form, expand: false)!.CanonicalText);
    }

    private static K3Rule Rule(string text, int arity)
    {
        return K3Rule.TryCreate(text, arity) ?? throw new InvalidOperationException($"'{text}' does not compile.");
    }

    private static CompiledRule<RuleTestContext> Nnf(string text)
    {
        return Rule(text, 3).Compiled.ToNnf().CompiledRule!;
    }

    private static CompiledRule<RuleTestContext> Dnf(string text)
    {
        return Rule(text, 3).Compiled.ToDnf().CompiledRule!;
    }

    private static CompiledRule<RuleTestContext>? Convert(CompiledRule<RuleTestContext> rule, string form, bool expand)
    {
        NormalFormOptions options = new(expand);
        return form switch
        {
            "nnf" => rule.ToNnf(options).CompiledRule,
            "cnf" => rule.ToCnf(options).CompiledRule,
            _ => rule.ToDnf(options).CompiledRule,
        };
    }
}
