namespace TruthWeaver.Tests;

using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Compile-time query validation (ADR-0006 decision 4, data-sources ticket 05): a source name declared with an
/// <see cref="IQueryValidator"/> turns a malformed query into a <c>TRE0025</c> diagnostic at the query string, in the DSL,
/// JSON and YAML; a name declared without one is not checked.
/// </summary>
public sealed class QueryValidationTests
{
    private const string Dsl = "ageAtLeast(min: from(\"user\", \"$.bad\"))";
    private const string Json = """{"predicate":"ageAtLeast","args":{"min":{"from":"user","query":"$.bad"}}}""";
    private const string Yaml = "predicate: ageAtLeast\nargs:\n  min:\n    from: user\n    query: \"$.bad\"\n";

    /// <summary>A declared validator that rejects the query makes the DSL rule fail with <c>TRE0025</c> at the query string.</summary>
    [Fact]
    public void Compile_MalformedQuery_ReportsTheValidatorsMessageAtTheQueryString_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Rejecting("unexpected token", 4));

        CompilationResult<RuleTestContext> result = compiler.Compile(Dsl);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedDataQuery, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("unexpected token", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("position 4", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(Dsl.IndexOf("\"$.bad\"", StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.Equal("\"$.bad\"".Length, diagnostic.Span.Length);
    }

    /// <summary>The same rule written as JSON is reported at the <c>query</c> member, by path and span.</summary>
    [Fact]
    public void CompileJson_MalformedQuery_PointsAtTheQueryMember_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Rejecting("unexpected token", 4));

        CompilationResult<RuleTestContext> result = compiler.CompileJson(Json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedDataQuery, diagnostic.Code);
        Assert.Equal("$.args.min.query", diagnostic.Path);
        Assert.Equal(Json.IndexOf("\"$.bad\"", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>The same rule written as YAML is reported at the <c>query</c> member, by path and span.</summary>
    [Fact]
    public void CompileYaml_MalformedQuery_PointsAtTheQueryMember_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Rejecting("unexpected token", 4));

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(Yaml);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedDataQuery, diagnostic.Code);
        Assert.Equal("$.args.min.query", diagnostic.Path);
        Assert.Equal(Yaml.IndexOf("\"$.bad\"", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>A validator that finds nothing wrong produces no diagnostic and the rule compiles.</summary>
    [Fact]
    public void Compile_QueryTheValidatorAccepts_ProducesNoDiagnostic_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate(Arg.Any<string>()).Returns([]);
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(validator);

        CompilationResult<RuleTestContext> result = compiler.Compile(Dsl);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>A source name declared without a validator is not syntax-checked, so any query text compiles.</summary>
    [Fact]
    public void Compile_SourceDeclaredWithoutAValidator_AcceptsAnyQuery_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(null);

        CompilationResult<RuleTestContext> result = compiler.Compile("ageAtLeast(min: from(\"user\", \"((not a query\"))");

        Assert.True(result.Succeeded);
    }

    /// <summary>The validator receives the query with the DSL's string escapes resolved, exactly what a source will be asked.</summary>
    [Fact]
    public void Compile_QueryWithEscapes_ValidatorReceivesTheUnescapedQuery_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate(Arg.Any<string>()).Returns([]);
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(validator);

        compiler.Compile("ageAtLeast(min: from(\"user\", \"$.a[?@.n==\\\"x\\\"]\"))");

        validator.Received(1).Validate("$.a[?@.n==\"x\"]");
    }

    /// <summary>Each problem the validator returns is its own diagnostic.</summary>
    [Fact]
    public void Compile_SeveralProblems_ReportsOneDiagnosticEach_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate(Arg.Any<string>()).Returns([new QueryProblem("first", 1), new QueryProblem("second")]);
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(validator);

        CompilationResult<RuleTestContext> result = compiler.Compile(Dsl);

        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == DiagnosticCodes.MalformedDataQuery));
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("first", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("second", StringComparison.Ordinal));
    }

    /// <summary>An undeclared source is reported once as undeclared; its query is not validated because no validator is known.</summary>
    [Fact]
    public void Compile_UndeclaredSource_DoesNotValidateTheQuery_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(validator);

        CompilationResult<RuleTestContext> result = compiler.Compile("ageAtLeast(min: from(\"other\", \"$.bad\"))");

        Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
        validator.DidNotReceiveWithAnyArgs().Validate(default!);
    }

    /// <summary>Every source name can carry its own validator; each query is checked by its own source's dialect.</summary>
    [Fact]
    public void Compile_TwoSources_EachQueryIsCheckedByItsOwnValidator_Test()
    {
        IQueryValidator strict = Rejecting("strict says no", 0);
        IQueryValidator lenient = Substitute.For<IQueryValidator>();
        lenient.Validate(Arg.Any<string>()).Returns([]);
        DataSourceDeclarations declarations = new() { ["strict"] = strict, ["lenient"] = lenient };
        RuleCompiler<RuleTestContext> compiler = CreateCompilerFor(declarations);

        CompilationResult<RuleTestContext> result = compiler.Compile(
            "ageAtLeast(min: from(\"strict\", \"q\")) AND ageAtLeast(min: from(\"lenient\", \"q\"))"
        );

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("strict says no", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>The declaration set records the validator a name was declared with and none for a bare name.</summary>
    [Fact]
    public void DataSourceDeclarations_NameWithAndWithoutValidator_ExposesEachNameAndValidator_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();

        DataSourceDeclarations declarations = new() { "bare" };
        declarations.Add("checked", validator);
        declarations["indexed"] = validator;

        Assert.Equal(["bare", "checked", "indexed"], declarations.Names.Order(StringComparer.Ordinal));
        Assert.True(declarations.Contains("bare"));
        Assert.Null(declarations["bare"]);
        Assert.Same(validator, declarations["checked"]);
        Assert.Same(validator, declarations["indexed"]);
        Assert.Null(declarations["unknown"]);
    }

    /// <summary>Declaring a bare name again never discards a validator that was already given.</summary>
    [Fact]
    public void DataSourceDeclarations_BareAddAfterValidator_KeepsTheValidator_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        DataSourceDeclarations declarations = [];
        declarations.Add("user", validator);

        declarations.Add("user");

        Assert.Same(validator, declarations["user"]);
    }

    private static IQueryValidator Rejecting(string message, int position)
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate(Arg.Any<string>()).Returns([new QueryProblem(message, position)]);
        return validator;
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler(IQueryValidator? validator)
    {
        DataSourceDeclarations declarations = [];
        if (validator is null)
        {
            declarations.Add("user");
        }
        else
        {
            declarations.Add("user", validator);
        }

        return CreateCompilerFor(declarations);
    }

    private static RuleCompiler<RuleTestContext> CreateCompilerFor(DataSourceDeclarations declarations)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "ageAtLeast",
                    "Age at least",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("min", "The minimum age.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
        return new RuleCompiler<RuleTestContext>(registry, new CompilerOptions(DataSources: declarations));
    }
}
