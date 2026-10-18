namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

public sealed class AssignmentSessionTests
{
    /// <summary>A term with no value is Unknown, and the Unknown operand shows how AND responds.</summary>
    [Fact]
    public async Task EvaluateAsync_NoValues_EveryTermIsUnknownAndTheTraceSaysWhy_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive AND isAdmin");
        AssignmentSession session = new();

        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.Unknown, outcome.Decision!.Result);
        Assert.False(outcome.IsSatisfied);
        Assert.Equal(["isActive", "isAdmin"], outcome.Terms.Select(t => t.Display));
        Assert.All(outcome.Terms, t => Assert.Equal(TruthValue.Unknown, t.Value));
        Assert.Contains("no operand is False", outcome.Reason, StringComparison.Ordinal);
        Assert.All(outcome.Trace!.Children, child => Assert.Contains("no value", child.Reason, StringComparison.Ordinal));
    }

    /// <summary>False wins over Unknown in AND.</summary>
    [Fact]
    public async Task EvaluateAsync_FalseOperandOfAnd_SettlesTheResultDespiteAnUnknownOperand_Test()
    {
        RuleDocument document = RuleDocument.Create("isAdmin AND isActive");
        AssignmentSession session = new();
        session.Set("isActive", TruthValue.False);

        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.False, outcome.Decision!.Result);
        Assert.Contains("False wins over an Unknown operand", outcome.Reason, StringComparison.Ordinal);
    }

    /// <summary>True wins over Unknown in OR.</summary>
    [Fact]
    public async Task EvaluateAsync_TrueOperandOfOr_SettlesTheResultDespiteAnUnknownOperand_Test()
    {
        RuleDocument document = RuleDocument.Create("isAdmin OR isActive");
        AssignmentSession session = new();
        session.Set("isActive", TruthValue.True);

        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.True, outcome.Decision!.Result);
        Assert.Contains("True wins over an Unknown operand", outcome.Reason, StringComparison.Ordinal);
    }

    /// <summary>NOT of Unknown stays Unknown, and the reason line says so.</summary>
    [Fact]
    public async Task EvaluateAsync_NotOfUnknown_StaysUnknown_Test()
    {
        RuleDocument document = RuleDocument.Create("NOT isActive");

        AssignmentOutcome outcome = await new AssignmentSession().EvaluateAsync(
            document,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, outcome.Decision!.Result);
        Assert.Equal("NOT is Unknown: NOT of Unknown stays Unknown.", outcome.Reason);
    }

    /// <summary>The decision equals the one that the stub evaluator gives for the same values.</summary>
    [Theory]
    [InlineData("True", "True")]
    [InlineData("True", "False")]
    [InlineData("False", "Unknown")]
    [InlineData("Unknown", "Unknown")]
    [InlineData("Unknown", "True")]
    public async Task EvaluateAsync_AnyAssignment_EqualsTheStubEvaluatorDecision_Test(string first, string second)
    {
        const string Text = "isActive OR NOT hasRole(role: \"editor\")";
        RuleDocument document = RuleDocument.Create(Text);
        AssignmentSession session = new();
        session.Set("isActive", Enum.Parse<TruthValue>(first));
        session.Set("hasRole(role: \"editor\")", Enum.Parse<TruthValue>(second));
        StubAssignment stubs = StubAssignment
            .Parse($"isActive: {first}\n'hasRole(role: \"editor\")': {second}\n", RuleFormat.Yaml)
            .Value!;
        ToolResult<StubEvaluation> expected = await StubEvaluator.EvaluateAsync(
            RuleLoader.LoadString(Text).Value!,
            new StubEvaluationOptions { Stubs = stubs },
            TestContext.Current.CancellationToken
        );

        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal(expected.Value!.Decision.Result, outcome.Decision!.Result);
        Assert.Equal(expected.Value.Decision.IsSatisfied, outcome.IsSatisfied);
        Assert.Equal(expected.Value.Decision.Faults.Count, outcome.Decision.Faults.Count);
    }

    /// <summary>Cycle moves a term through True, False and Unknown, and Unknown removes the stored value.</summary>
    [Fact]
    public void Cycle_Term_MovesThroughTheThreeValues_Test()
    {
        AssignmentSession session = new();

        Assert.Equal(TruthValue.True, session.Cycle("isActive"));
        Assert.Equal(TruthValue.False, session.Cycle("isActive"));
        Assert.Equal(TruthValue.Unknown, session.Cycle("isActive"));
        Assert.Empty(session.Values);
    }

    /// <summary>The policy moves through the three collapse policies and the projection follows it.</summary>
    [Fact]
    public async Task CyclePolicy_Policies_ChangeCollapseAndProjection_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive");
        AssignmentSession session = new();

        AssignmentOutcome asFalse = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);
        _ = session.CyclePolicy();
        AssignmentOutcome asTrue = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);
        _ = session.CyclePolicy();
        AssignmentOutcome isError = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal((CollapseOutcome.False, TruthValue.False), (asFalse.Collapsed!.Value, asFalse.Projected!.Value));
        Assert.Equal((CollapseOutcome.True, TruthValue.True), (asTrue.Collapsed!.Value, asTrue.Projected!.Value));
        Assert.Equal(CollapseOutcome.RejectedUnresolved, isError.Collapsed);
        Assert.Null(isError.Projected);
    }

    /// <summary>A data reference with no variables is Unknown with a fault, and the leaf says why.</summary>
    [Fact]
    public async Task EvaluateAsync_MissingVariable_IsUnknownWithItsFault_Test()
    {
        RuleDocument document = RuleDocument.Create("inRegion(region: from(\"user\", \"$.region\"))");

        AssignmentOutcome outcome = await new AssignmentSession().EvaluateAsync(
            document,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, outcome.Decision!.Result);
        string fault = Assert.Single(outcome.Faults);
        Assert.Contains("user", fault, StringComparison.Ordinal);
        Assert.Contains("could not be evaluated", outcome.Trace!.Reason, StringComparison.Ordinal);
        TermAssignment term = Assert.Single(outcome.Terms);
        Assert.True(term.HasDataReference);
        Assert.Null(term.Key);
    }

    /// <summary>With the variables loaded, a data term takes the key of the term with the value that the data gives.</summary>
    [Fact]
    public async Task EvaluateAsync_VariablesLoaded_DataTermUsesTheResolvedKey_Test()
    {
        RuleDocument document = RuleDocument.Create("inRegion(region: from(\"user\", \"$.region\"))");
        AssignmentSession session = new();
        Assert.True(session.SetVariables("{\"user\": {\"region\": \"EU\"}}"));

        AssignmentOutcome first = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);
        TermAssignment term = Assert.Single(first.Terms);
        session.Set(term.Key!, TruthValue.True);
        AssignmentOutcome second = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal("inRegion(region: \"EU\")", term.Key);
        Assert.Equal(TruthValue.True, second.Decision!.Result);
        Assert.True(second.IsSatisfied);
        Assert.Empty(second.Faults);
    }

    /// <summary>YAML variables read the same as JSON ones.</summary>
    [Fact]
    public async Task SetVariables_Yaml_GivesTheSameDataAsJson_Test()
    {
        RuleDocument document = RuleDocument.Create("inRegion(region: from(\"user\", \"$.region\"))");
        AssignmentSession session = new();
        Assert.True(session.SetVariables("user:\n  region: EU\n"));

        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.Equal("inRegion(region: \"EU\")", Assert.Single(outcome.Terms).Key);
    }

    /// <summary>Variables with an error keep the last well-formed data and report the problem.</summary>
    [Fact]
    public async Task SetVariables_Malformed_KeepsTheLastGoodDataAndReports_Test()
    {
        RuleDocument document = RuleDocument.Create("inRegion(region: from(\"user\", \"$.region\"))");
        AssignmentSession session = new();
        Assert.True(session.SetVariables("{\"user\": {\"region\": \"EU\"}}"));

        bool ok = session.SetVariables("{\"user\": {\"region\": ");
        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.False(ok);
        Assert.NotEmpty(outcome.VariableDiagnostics);
        Assert.Equal("inRegion(region: \"EU\")", Assert.Single(outcome.Terms).Key);
    }

    /// <summary>While the text has an error, the last valid tree is the one evaluated.</summary>
    [Fact]
    public async Task EvaluateAsync_TextWithErrors_EvaluatesTheLastValidTree_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive");
        document.SetText("isActive AND (");
        AssignmentSession session = new();
        session.Set("isActive", TruthValue.True);

        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        Assert.False(outcome.IsRuleCurrent);
        Assert.True(outcome.IsSatisfied);
    }

    /// <summary>A document that never compiled has nothing to evaluate.</summary>
    [Fact]
    public async Task EvaluateAsync_NoRule_ReportsNoRule_Test()
    {
        RuleDocument document = RuleDocument.Create("isActive AND (");

        AssignmentOutcome outcome = await new AssignmentSession().EvaluateAsync(
            document,
            TestContext.Current.CancellationToken
        );

        Assert.False(outcome.HasRule);
        Assert.Null(outcome.Decision);
        Assert.False(outcome.IsSatisfied);
    }

    /// <summary>Load replaces the values and the variables, as opening a saved case does.</summary>
    [Fact]
    public void Load_ValuesAndVariables_ReplacesTheAssignment_Test()
    {
        AssignmentSession session = new();
        session.Set("old", TruthValue.True);

        session.Load([new KeyValuePair<string, TruthValue>("isActive", TruthValue.False)], "{\"user\": {}}");

        Assert.Equal(TruthValue.False, session.ValueOf("isActive"));
        Assert.Equal(TruthValue.Unknown, session.ValueOf("old"));
        Assert.Equal("{\"user\": {}}", session.VariablesText);
    }
}
