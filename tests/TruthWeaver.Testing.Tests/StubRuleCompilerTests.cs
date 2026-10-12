namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

public sealed class StubRuleCompilerTests
{
    /// <summary>Each stand-in gives the stub value of its term, and the rule combines them in Strong Kleene logic.</summary>
    [Fact]
    public async Task Compile_StubValues_DecideTheResult_Test()
    {
        CompiledRule<StubValues> rule = StubRuleCompiler
            .Compile("isActive AND hasRole(role: \"editor\")", RuleFormat.Text)
            .GetRuleOrThrow();
        StubValues stubs = StubValues.Create(
            new Dictionary<string, TruthValue>
            {
                ["isActive"] = TruthValue.True,
                ["hasRole(role:\"editor\")"] = TruthValue.False,
            }
        );

        Decision decision = await rule.EvaluateAsync(stubs, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>A term with no stub value is Unknown, with no fault.</summary>
    [Fact]
    public async Task Compile_TermWithoutStub_IsUnknownWithoutFault_Test()
    {
        CompiledRule<StubValues> rule = StubRuleCompiler.Compile("isActive OR isAdmin", RuleFormat.Text).GetRuleOrThrow();

        Decision decision = await rule.EvaluateAsync(
            StubValues.Empty,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A from(...) argument takes the value of the data source, and the stub of the term with that value applies.</summary>
    [Fact]
    public async Task Compile_FromReference_UsesTheStubOfTheResolvedTerm_Test()
    {
        CompiledRule<StubValues> rule = StubRuleCompiler
            .Compile("inRegion(region: from(\"user\", \"$.region\"))", RuleFormat.Text)
            .GetRuleOrThrow();
        StubValues stubs = StubValues.Create(
            new Dictionary<string, TruthValue> { ["inRegion(region: \"EU\")"] = TruthValue.True }
        );
        DataSourceSet sources = [];
        sources.Add("user", YamlDataSource.Parse("region: EU"));

        Decision decision = await rule.EvaluateAsync(
            stubs,
            dataSources: sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The same rule as a JSON tree compiles over the stand-ins too.</summary>
    [Fact]
    public async Task Compile_JsonRule_EvaluatesLikeRuleText_Test()
    {
        CompiledRule<StubValues> rule = StubRuleCompiler
            .Compile("""{"op":"NOT","operands":[{"predicate":"isActive"}]}""", RuleFormat.Json)
            .GetRuleOrThrow();
        StubValues stubs = StubValues.Create(new Dictionary<string, TruthValue> { ["isActive"] = TruthValue.True });

        Decision decision = await rule.EvaluateAsync(stubs, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>A syntax error is an error diagnostic, and the Try form returns false.</summary>
    [Fact]
    public void TryCompile_SyntaxError_ReturnsFalseWithAnError_Test()
    {
        bool compiled = StubRuleCompiler.TryCompile(
            "isActive AND",
            RuleFormat.Text,
            out CompiledRule<StubValues>? rule,
            out EquatableArray<Diagnostic> diagnostics
        );

        Assert.False(compiled);
        Assert.Null(rule);
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
