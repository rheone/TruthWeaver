namespace TruthWeaver.Tool.Core.Tests.Assignment;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Yaml;

/// <summary>Finds a term's outcome by tree path, source span and call text, with its faults attributed by term identity.</summary>
public sealed class TermOutcomeLookupTests
{
    /// <summary>Two terms of the same predicate with different arguments are distinct terms, each with its own outcome.</summary>
    [Fact]
    public async Task FindByCall_SameNameDifferentArguments_ResolveToDifferentTerms_Test()
    {
        const string Text = "isAdult(age: 18) AND isAdult(age: 21)";
        RuleDocument document = RuleDocument.Create(Text);
        AssignmentSession session = new();
        session.Set("isAdult(age: 18)", TruthValue.True);
        session.Set("isAdult(age: 21)", TruthValue.False);
        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);

        TermOutcome? first = TermOutcomeLookup.FindByCall(outcome, "isAdult(age: 18)");
        TermOutcome? second = TermOutcomeLookup.FindByCall(outcome, "isAdult(age: 21)");

        Assert.Equal(TruthValue.True, first?.Value);
        Assert.Equal(TruthValue.False, second?.Value);
    }

    /// <summary>A tree path resolves to the same outcome as the term's call text.</summary>
    [Fact]
    public async Task FindByPath_TermNode_ResolvesTheSameTermAsItsCallText_Test()
    {
        const string Text = "isAdult(age: 18) AND isAdult(age: 21)";
        RuleDocument document = RuleDocument.Create(Text);
        AssignmentSession session = new();
        session.Set("isAdult(age: 21)", TruthValue.True);
        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);
        RuleTree tree = RuleTree.Of(Compile(Text));

        TermOutcome? byPath = TermOutcomeLookup.FindByPath(tree, outcome, "$.operands[1]");

        Assert.NotNull(byPath);
        Assert.Equal("isAdult(age: 21)", byPath.Display);
        Assert.Equal(TruthValue.True, byPath.Value);
    }

    /// <summary>A from(...) call whose query string has an escaped quote resolves to the right term by source span.</summary>
    [Fact]
    public async Task FindBySpan_FromCallWithEscapedQuote_ResolvesTheRightTerm_Test()
    {
        const string Text = "inRegion(region: from(\"user\", \"$.a\\\"b\"))";
        RuleDocument document = RuleDocument.Create(Text);
        AssignmentSession session = new();
        AssignmentOutcome outcome = await session.EvaluateAsync(document, TestContext.Current.CancellationToken);
        TermAssignment term = Assert.Single(outcome.Terms);

        // An offset inside the escaped-quote argument, well before the call's own closing parenthesis.
        int offset = Text.IndexOf("a\\\"b", StringComparison.Ordinal);
        TermOutcome? found = TermOutcomeLookup.FindBySpan(Text, outcome, new SourceSpan(offset, 0));

        Assert.NotNull(found);
        Assert.Equal(term.Display, found.Display);
    }

    /// <summary>A fault whose message starts with the display text of another term is still attributed to the faulting term.</summary>
    [Fact]
    public void FindByCall_FaultMessageStartsWithAnotherTermsDisplay_AttributesTheFaultToTheFaultingTerm_Test()
    {
        TermIdentity faultingTerm = new("a", []);
        Fault fault = new(faultingTerm, new InvalidOperationException("b: looks like the other term, but is not"));
        Decision decision = new(TruthValue.Unknown, new EquatableArray<Fault>([fault]));
        AssignmentOutcome outcome = new(
            HasRule: true,
            IsRuleCurrent: true,
            Terms:
            [
                new TermAssignment("a", "a", TruthValue.Unknown, HasDataReference: false),
                new TermAssignment("b", "b", TruthValue.Unknown, HasDataReference: false),
            ],
            Decision: decision,
            Trace: null,
            Reason: string.Empty,
            Faults: [$"{faultingTerm}: {fault.Exception.Message}"],
            Policy: CollapsePolicy.UnknownAsFalse,
            Collapsed: null,
            Projected: null,
            VariableDiagnostics: []
        )
        {
            Evaluation = new StubEvaluation(Compile("a AND b"), decision, new EvaluationOptions()),
        };

        TermOutcome? faultingOutcome = TermOutcomeLookup.FindByCall(outcome, "a");
        TermOutcome? otherOutcome = TermOutcomeLookup.FindByCall(outcome, "b");

        Assert.Equal([fault], faultingOutcome?.Faults);
        Assert.Empty(otherOutcome!.Faults);
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        CompilationResult<StubAssignment> result = RuleConverter.CreateCompiler().CompileAs(text, RuleFormat.Text);
        Assert.NotNull(result.Rule);
        return result.Rule;
    }
}
