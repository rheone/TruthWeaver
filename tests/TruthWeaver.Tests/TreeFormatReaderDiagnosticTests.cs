namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// What the JSON and YAML tree reader reports for a malformed node: the message, the element the span covers, the
/// expected-versus-found pair and the path, so an author sees which member to fix.
/// </summary>
public sealed class TreeFormatReaderDiagnosticTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddStringArgPredicate("hasRole", "role", "Y")
            .Build()
    );

    /// <summary>An <c>op</c> that is not a string is reported at the op value.</summary>
    [Fact]
    public void CompileJson_OpThatIsNotAString_ReportsTheOpValue_Test()
    {
        const string json = """{"op":5}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("'op' must be a JSON string naming an operator.", diagnostic.Message);
        Assert.Equal("5", Covered(json, diagnostic));
        Assert.Equal("a JSON string", diagnostic.Expected);
        Assert.Equal("$.op", diagnostic.Path);
    }

    /// <summary>A <c>predicate</c> that is not a string is reported at the predicate value and its path.</summary>
    [Fact]
    public void CompileJson_PredicateThatIsNotAString_ReportsThePredicateValue_Test()
    {
        const string json = """{"predicate":5}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("'predicate' must be a JSON string.", diagnostic.Message);
        Assert.Equal("a JSON string", diagnostic.Expected);
        Assert.Equal("$.predicate", diagnostic.Path);
        Assert.Equal("5", Covered(json, diagnostic));
    }

    /// <summary>An <c>args</c> that is not an object is reported at the args value and its path.</summary>
    [Fact]
    public void CompileJson_ArgsThatIsNotAnObject_ReportsTheArgsValue_Test()
    {
        const string json = """{"predicate":"hasRole","args":5}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("a JSON object", diagnostic.Expected);
        Assert.Equal("$.args", diagnostic.Path);
        Assert.Equal("5", Covered(json, diagnostic));
    }

    /// <summary>A threshold without <c>k</c> is reported at the node and says that the key is missing.</summary>
    [Fact]
    public void CompileJson_ThresholdWithoutK_ReportsTheNodeAndTheMissingKey_Test()
    {
        const string json = """{"op":"atLeast","operands":[{"const":true}]}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("'atLeast' requires a numeric 'k'.", diagnostic.Message);
        Assert.Equal(json, Covered(json, diagnostic));
        Assert.Equal("an integer", diagnostic.Expected);
        Assert.Equal("no 'k' property", diagnostic.Found);
        Assert.Equal("$", diagnostic.Path);
    }

    /// <summary>A threshold whose <c>k</c> is not an integer is reported at the k value and names what was found.</summary>
    [Fact]
    public void CompileJson_ThresholdWithANonIntegerK_ReportsTheKValue_Test()
    {
        const string json = """{"op":"atLeast","k":"two","operands":[{"const":true}]}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("\"two\"", Covered(json, diagnostic));
        Assert.Equal("$.k", diagnostic.Path);
        Assert.NotEqual("no 'k' property", diagnostic.Found);
    }

    /// <summary>A BETWEEN without <c>min</c> is reported at the node and the rule does not compile.</summary>
    [Fact]
    public void CompileJson_BetweenWithoutMin_ReportsTheNodeAndDoesNotCompile_Test()
    {
        const string json = """{"op":"between","max":2,"operands":[{"const":true},{"const":false}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.False(result.Succeeded);
        Assert.Equal("'between' requires integer 'min' and 'max'.", diagnostic.Message);
        Assert.Equal(json, Covered(json, diagnostic));
        Assert.Equal("no 'min' property", diagnostic.Found);
        Assert.Equal("$", diagnostic.Path);
    }

    /// <summary>A BETWEEN whose <c>max</c> is not an integer is reported at the max value.</summary>
    [Fact]
    public void CompileJson_BetweenWithANonIntegerMax_ReportsTheMaxValue_Test()
    {
        const string json = """{"op":"between","min":0,"max":"x","operands":[{"const":true},{"const":false}]}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("\"x\"", Covered(json, diagnostic));
        Assert.Equal("$.max", diagnostic.Path);
        Assert.NotEqual("no 'max' property", diagnostic.Found);
    }

    /// <summary>A variable reference with a member other than <c>from</c> and <c>query</c> is reported at that member.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithAnExtraMember_ReportsTheMember_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":{"from":"s","query":"q","extra":1}}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("A variable reference has only 'from' and 'query' members, but found 'extra'.", diagnostic.Message);
        Assert.Equal("1", Covered(json, diagnostic));
        Assert.Equal("only 'from' and 'query'", diagnostic.Expected);
        Assert.Equal("'extra'", diagnostic.Found);
        Assert.Equal("$.args.role.extra", diagnostic.Path);
    }

    /// <summary>A variable reference with a non-string key is reported at the key, at the reference's own path.</summary>
    [Fact]
    public void CompileYaml_VariableReferenceWithANonStringKey_ReportsTheKey_Test()
    {
        const string yaml = "predicate: hasRole\nargs:\n  role:\n    from: s\n    ? [1]\n    : x\n";

        Diagnostic diagnostic = SingleYaml(yaml);

        Assert.Equal("$.args.role", diagnostic.Path);
        Assert.Equal("only 'from' and 'query'", diagnostic.Expected);
        Assert.Equal(yaml.IndexOf("[1]", StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.StartsWith("A variable reference has only 'from' and 'query' members, but found", diagnostic.Message);
        Assert.DoesNotContain("'", diagnostic.Found);
    }

    /// <summary>A variable reference without <c>from</c> is reported at the reference.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithoutFrom_ReportsTheReference_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":{"query":"q"}}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("A variable reference requires a 'from' property holding the source name.", diagnostic.Message);
        Assert.Equal("a 'from' property", diagnostic.Expected);
        Assert.Equal("no 'from' property", diagnostic.Found);
        Assert.Equal("$.args.role", diagnostic.Path);
        Assert.Equal("""{"query":"q"}""", Covered(json, diagnostic));
    }

    /// <summary>A variable reference without <c>query</c> is reported at the reference.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithoutQuery_ReportsTheReference_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":{"from":"s"}}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("A variable reference requires a 'query' property holding the query.", diagnostic.Message);
        Assert.Equal("no 'query' property", diagnostic.Found);
        Assert.Equal("$.args.role", diagnostic.Path);
    }

    /// <summary>A variable reference whose <c>from</c> is not a string is reported at the from value.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithANonStringFrom_ReportsTheFromValue_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":{"from":5,"query":"q"}}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("The 'from' of a variable reference must be a JSON string holding the source name.", diagnostic.Message);
        Assert.Equal("a JSON string", diagnostic.Expected);
        Assert.Equal("$.args.role.from", diagnostic.Path);
        Assert.Equal("5", Covered(json, diagnostic));
    }

    /// <summary>A variable reference whose <c>query</c> is not a string is reported at the query value.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithANonStringQuery_ReportsTheQueryValue_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":{"from":"s","query":5}}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("The 'query' of a variable reference must be a JSON string holding the query.", diagnostic.Message);
        Assert.Equal("$.args.role.query", diagnostic.Path);
        Assert.Equal("5", Covered(json, diagnostic));
    }

    /// <summary>An array argument whose second item is not a literal is located at that item's index.</summary>
    [Fact]
    public void CompileJson_ArrayArgumentWithABadSecondItem_ReportsTheItemIndex_Test()
    {
        const string json = """{"predicate":"hasRole","args":{"role":["a",null]}}""";

        Diagnostic diagnostic = Single(json);

        Assert.Equal("$.args.role[1]", diagnostic.Path);
    }

    /// <summary>A YAML threshold whose <c>k</c> is not an integer is underlined at the k value, not the whole node.</summary>
    [Fact]
    public void CompileYaml_ThresholdWithANonIntegerK_SpanCoversTheKValue_Test()
    {
        const string yaml = "op: atLeast\nk: two\noperands:\n  - const: true\n";

        Diagnostic diagnostic = SingleYaml(yaml);

        Assert.Equal("two", Covered(yaml, diagnostic));
        Assert.Equal("'two'", diagnostic.Found);
    }

    /// <summary>A YAML BETWEEN whose <c>min</c> is not an integer is underlined at the min value.</summary>
    [Fact]
    public void CompileYaml_BetweenWithANonIntegerMin_SpanCoversTheMinValue_Test()
    {
        const string yaml = "op: between\nmin: x\nmax: 1\noperands:\n  - const: true\n  - const: false\n";

        Diagnostic diagnostic = SingleYaml(yaml);

        Assert.Equal("x", Covered(yaml, diagnostic));
        Assert.Equal("'x'", diagnostic.Found);
    }

    /// <summary>A BETWEEN with neither bound reports only the first missing bound.</summary>
    [Fact]
    public void CompileJson_BetweenWithNoBounds_ReportsOnlyTheMinimum_Test()
    {
        Diagnostic diagnostic = Single("""{"op":"between","operands":[{"const":true},{"const":false}]}""");

        Assert.Equal("no 'min' property", diagnostic.Found);
    }

    /// <summary>A variable reference with neither member reports only the missing <c>from</c>.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithoutFromAndQuery_ReportsOnlyFrom_Test()
    {
        Diagnostic diagnostic = Single("""{"predicate":"hasRole","args":{"role":{}}}""");

        Assert.Equal("no 'from' property", diagnostic.Found);
    }

    /// <summary>A variable reference with a bad <c>from</c> and a missing <c>query</c> reports only the bad <c>from</c>.</summary>
    [Fact]
    public void CompileJson_VariableReferenceWithBadFromAndNoQuery_ReportsOnlyFrom_Test()
    {
        Diagnostic diagnostic = Single("""{"predicate":"hasRole","args":{"role":{"from":5}}}""");

        Assert.Equal("$.args.role.from", diagnostic.Path);
    }

    /// <summary>The retired NXOR op is rejected with PARITY as the expected word.</summary>
    [Fact]
    public void CompileJson_NxorOp_ExpectsParity_Test()
    {
        Diagnostic diagnostic = Single("""{"op":"nxor","operands":[{"const":true},{"const":false}]}""");

        Assert.Equal("PARITY", diagnostic.Expected);
        Assert.Equal("parity", diagnostic.Suggestion?.Text);
    }

    private static Diagnostic Single(string json)
    {
        return Assert.Single(Compiler.CompileJson(json).Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static Diagnostic SingleYaml(string yaml)
    {
        return Assert.Single(Compiler.CompileYaml(yaml).Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static string Covered(string text, Diagnostic diagnostic)
    {
        return text.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
    }
}
