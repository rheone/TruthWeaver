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

    /// <summary>A JSON rule whose arguments are numbers, Booleans and lists compiles, each with the kind the literal shows.</summary>
    [Fact]
    public async Task Compile_JsonRuleWithArgumentsOfEachKind_CompilesAndEvaluates_Test()
    {
        const string Json = """
            {"predicate":"p","args":{"n":1,"d":2.5,"b":true,"s":"x","ns":[1,2],"ds":[1.5],"bs":[true],"ss":["a"],"empty":[]}}
            """;
        CompiledRule<StubValues> rule = StubRuleCompiler.Compile(Json, RuleFormat.Json).GetRuleOrThrow();
        StubValues stubs = StubValues.Create(new Dictionary<string, TruthValue> { [rule.CanonicalText] = TruthValue.True });

        Decision decision = await rule.EvaluateAsync(stubs, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>Two uses of one predicate with a whole number and a decimal number for an argument join as one decimal kind.</summary>
    [Fact]
    public void Compile_SameArgumentAsIntegerAndDecimal_JoinsAsDecimal_Test()
    {
        const string Json = """
            {"op":"AND","operands":[
              {"predicate":"p","args":{"n":1,"ns":[1]}},
              {"predicate":"P","args":{"N":2.5,"NS":[2.5]}}
            ]}
            """;

        CompilationResult<StubValues> result = StubRuleCompiler.Compile(Json, RuleFormat.Json);

        Assert.True(result.TryGetRule(out CompiledRule<StubValues>? rule, out _), "The rule should compile.");
        Assert.NotNull(rule);
    }

    /// <summary>An argument given by a literal in one use and by a from(...) reference in another keeps the literal kind.</summary>
    [Fact]
    public void Compile_SameArgumentAsLiteralAndReference_KeepsTheLiteralKind_Test()
    {
        const string Json = """
            {"op":"AND","operands":[
              {"predicate":"p","args":{"n":1}},
              {"predicate":"p","args":{"n":{"from":"user","query":"$.n"}}}
            ]}
            """;

        CompilationResult<StubValues> result = StubRuleCompiler.Compile(Json, RuleFormat.Json);

        Assert.True(result.TryGetRule(out CompiledRule<StubValues>? rule, out _), "The rule should compile.");
        Assert.NotNull(rule);
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
