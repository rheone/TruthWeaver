namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// <c>CompiledRule.Explain</c>: the fixed-English sentence for a decision. Each context is a string such as
/// <c>"type=T,total=F,vip=X"</c>: <c>T</c>, <c>F</c> and <c>U</c> are the answer of the predicate, <c>X</c> makes it
/// throw (a fault), and a missing name answers <c>Unknown</c>.
/// </summary>
public sealed class ExplainTests
{
    /// <summary>A true result names the terms that decided it, with the schema label and the argument values.</summary>
    [Fact]
    public async Task Explain_TrueResult_NamesTheDecidingTermsWithLabelsAndArguments_Test()
    {
        CompiledRule<string> rule = Compile("""orderType(kind: "takeout") AND overLimit()""");
        Decision decision = await EvaluateAsync(rule, "orderType=T,overLimit=T");

        string sentence = rule.Explain(decision);

        Assert.Equal("""Satisfied because Order Type (kind: "takeout") is true and Over Limit is true.""", sentence);
    }

    /// <summary>A term that did not decide the result is left out of the sentence.</summary>
    [Fact]
    public async Task Explain_TermThatDidNotDecide_IsNotNamed_Test()
    {
        CompiledRule<string> rule = Compile("vip() OR overLimit()");
        Decision decision = await EvaluateAsync(rule, "vip=T,overLimit=F");

        string sentence = rule.Explain(decision, new ExplainOptions());

        Assert.Equal("Satisfied because VIP is true.", sentence);
    }

    /// <summary>An unknown result has no deciding term, so the sentence names the unknown term.</summary>
    [Fact]
    public async Task Explain_UnknownResult_NamesTheUnknownTerms_Test()
    {
        CompiledRule<string> rule = Compile("vip() AND overLimit()");
        Decision decision = await EvaluateAsync(rule, "vip=T,overLimit=U");

        string sentence = rule.Explain(decision);

        Assert.Equal("Unknown because Over Limit is unknown.", sentence);
    }

    /// <summary>A term whose predicate faulted is reported as faulted, not as "not evaluated" (that wording is for a short-circuit skip), without the exception text.</summary>
    [Fact]
    public async Task Explain_FaultedTerm_SaysItFaulted_Test()
    {
        CompiledRule<string> rule = Compile("vip() AND overLimit()");
        Decision decision = await EvaluateAsync(rule, "vip=T,overLimit=X");

        string sentence = rule.Explain(decision);

        Assert.Equal("Unknown because Over Limit faulted.", sentence);
        Assert.DoesNotContain("evaluated", sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// A quantifier term that decides the result is named in the explanation sentence by its own canonical text
    /// (it has no predicate schema of its own to supply a nicer label), not left out or blank.
    /// </summary>
    [Fact]
    public async Task Explain_QuantifierDecidesTheResult_NamesTheQuantifierAtomByItsCanonicalText_Test()
    {
        PredicateRegistryBuilder<QuantifierExplainContext> builder = PredicateRegistry<QuantifierExplainContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                ctx => ctx.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        CompiledRule<QuantifierExplainContext> rule = new RuleCompiler<QuantifierExplainContext>(builder.Build())
            .Compile("FORALL(x IN items, elementTrue)")
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new QuantifierExplainContext { Items = [TruthValue.True, TruthValue.True] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        string sentence = rule.Explain(decision);

        Assert.Equal("Satisfied because FORALL(x IN items, elementTrue) is true.", sentence);
    }

    /// <summary>A rule with no term gives only the head.</summary>
    [Fact]
    public async Task Explain_RuleWithoutTerms_GivesOnlyTheResult_Test()
    {
        CompiledRule<string> rule = Compile("TRUE");
        Decision decision = await EvaluateAsync(rule, string.Empty);

        string sentence = rule.Explain(decision);

        Assert.Equal("Satisfied.", sentence);
    }

    /// <summary>A host label replaces the schema label, whatever the case of the predicate name.</summary>
    [Fact]
    public async Task Explain_LabelOverride_ReplacesTheSchemaLabel_Test()
    {
        CompiledRule<string> rule = Compile("vip() AND overLimit()");
        Decision decision = await EvaluateAsync(rule, "vip=T,overLimit=F");
        ExplainOptions options = new() { Labels = new Dictionary<string, string> { ["OVERLIMIT"] = "the total" } };

        string sentence = rule.Explain(decision, options);

        Assert.Equal("Not satisfied because the total is false.", sentence);
    }

    /// <summary>The descriptions are added only when the option asks for them.</summary>
    [Fact]
    public async Task Explain_IncludeDescriptions_AppendsTheDescriptionInBrackets_Test()
    {
        CompiledRule<string> rule = Compile("vip() AND overLimit()");
        Decision decision = await EvaluateAsync(rule, "vip=T,overLimit=F");

        string sentence = rule.Explain(decision, new ExplainOptions { IncludeDescriptions = true });

        Assert.Equal("Not satisfied because Over Limit is false [Is the total over the limit?].", sentence);
    }

    /// <summary>A decision without a trace tree is rejected, as the other decision printers do.</summary>
    [Fact]
    public void Explain_DecisionWithoutTraceTree_ThrowsArgumentException_Test()
    {
        CompiledRule<string> rule = Compile("vip()");

        Assert.Throws<ArgumentException>(() => rule.Explain(new Decision(TruthValue.True, [])));
    }

    /// <summary>A null decision is rejected.</summary>
    [Fact]
    public void Explain_NullDecision_ThrowsArgumentNullException_Test()
    {
        CompiledRule<string> rule = Compile("vip()");

        Assert.Throws<ArgumentNullException>(() => rule.Explain(null!));
    }

    private static CompiledRule<string> Compile(string text)
    {
        PredicateRegistryBuilder<string> builder = PredicateRegistry<string>.CreateBuilder();
        builder.Add(
            new PredicateSchema(
                "orderType",
                "Order Type",
                "Is the order of the given kind?",
                [new PredicateArgumentSchema("kind", "The kind of order.", LiteralKind.String)]
            ),
            (context, _, _) => ValueTask.FromResult(Read(context, "orderType"))
        );
        builder.Add(
            PredicateSchema.NoArguments("overLimit", "Over Limit", "Is the total over the limit?"),
            (context, _, _) => ValueTask.FromResult(Read(context, "overLimit"))
        );
        builder.Add(
            PredicateSchema.NoArguments("vip", "VIP", "Is the customer a VIP?"),
            (context, _, _) => ValueTask.FromResult(Read(context, "vip"))
        );

        CompilationResult<string> result = new RuleCompiler<string>(builder.Build()).Compile(text);
        return result.CompiledRule ?? throw new InvalidOperationException(result.FormatDiagnostics(text));
    }

    private static TruthValue Read(string context, string name)
    {
        foreach (string pair in context.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=');
            if (parts[0] == name)
            {
                return parts[1] switch
                {
                    "T" => TruthValue.True,
                    "F" => TruthValue.False,
                    "U" => TruthValue.Unknown,
                    _ => throw new InvalidOperationException($"'{name}' faulted."),
                };
            }
        }

        return TruthValue.Unknown;
    }

    private static Task<Decision> EvaluateAsync(CompiledRule<string> rule, string context)
    {
        return rule.EvaluateAsync(context, cancellationToken: TestContext.Current.CancellationToken);
    }

    private sealed class QuantifierExplainContext
    {
        public TruthValue[]? Items { get; set; }
    }
}
