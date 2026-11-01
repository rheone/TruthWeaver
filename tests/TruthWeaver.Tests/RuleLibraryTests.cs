namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 11b-2: a <see cref="RuleLibrary"/> of named rules, and <c>rule("x")</c> references in rule text that expand the
/// library rule inline at compile time, observed through <see cref="RuleCompiler{TContext}.Compile(string)"/>.
/// </summary>
public sealed class RuleLibraryTests
{
    /// <summary>
    /// A <c>rule("x")</c> reference compiles to the same tree as the library rule written out by hand, and evaluates to
    /// the same value.
    /// </summary>
    [Fact]
    public async Task Compile_LibraryRuleReference_ExpandsToTheLibraryRuleTreeAndEvaluatesTheSame_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library(("adult-customer", "a AND NOT b")));

        CompiledRule<RuleTestContext> direct = Compile(compiler, "a AND NOT b");
        CompiledRule<RuleTestContext> referenced = Compile(compiler, "rule(\"adult-customer\")");

        Assert.Equal(direct.CanonicalText, referenced.CanonicalText);
        Decision expected = await direct.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision actual = await referenced.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(expected.Result, actual.Result);
    }

    /// <summary>A reference pinned to the current content hash of the library rule compiles to the library rule's tree.</summary>
    [Fact]
    public void Compile_PinMatchesTheContentHash_ExpandsTheLibraryRule_Test()
    {
        RuleLibrary library = Library(("adult", "a AND b"));
        Assert.True(library.TryGet("adult", out LibraryRule? adult));
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(library);

        CompiledRule<RuleTestContext> rule = Compile(compiler, $"rule(\"adult\", hash: \"{adult.Hash}\") OR c");

        Assert.Equal(Compile(compiler, "(a AND b) OR c").CanonicalText, rule.CanonicalText);
    }

    /// <summary>
    /// A reference pinned to a hash that is not the current content hash of the library rule fails the compile with one
    /// diagnostic that names the rule, the pinned hash and the current hash.
    /// </summary>
    [Fact]
    public void Compile_PinDoesNotMatchTheContentHash_ReportsTheRuleAndBothHashes_Test()
    {
        RuleLibrary library = Library(("adult", "a AND b"));
        Assert.True(library.TryGet("adult", out LibraryRule? adult));
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(library);
        string stale = new('0', 64);

        CompilationResult<RuleTestContext> result = compiler.Compile($"rule(\"adult\", hash: \"{stale}\")");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.LibraryRuleHashMismatch, diagnostic.Code);
        Assert.Contains("'adult'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(stale, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(adult.Hash, diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The content hash of a library rule is the hash the rule store computes: SHA-256 of the UTF-8 bytes of the rule
    /// text, as upper-case hexadecimal digits.
    /// </summary>
    [Fact]
    public void Hash_LibraryRule_IsTheSha256OfTheRuleTextInUpperCaseHex_Test()
    {
        const string text = "a AND b";

        Assert.True(Library(("adult", text)).TryGet("ADULT", out LibraryRule? adult));

        string expected = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))
        );
        Assert.Equal(expected, adult.Hash);
    }

    /// <summary>
    /// Library rules that reference each other in a cycle fail the compile with exactly one diagnostic, which lists the
    /// cycle path.
    /// </summary>
    [Fact]
    public void Compile_CycleAcrossLibraryRules_ReportsOneDiagnosticListingThePath_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(
            Library(("first", "rule(\"second\") AND a"), ("second", "b OR rule(\"first\")"))
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("c AND rule(\"first\")");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.LibraryRuleCycleDetected, diagnostic.Code);
        Assert.Contains("first -> second -> first", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A reference to a name that the library does not hold, or a reference when the compiler has no library, fails the
    /// compile with a diagnostic that names the rule and covers the reference.
    /// </summary>
    /// <param name="hasLibrary">Whether the compiler has a library (one that holds other rules).</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compile_ReferenceToARuleTheLibraryDoesNotHold_ReportsUnknownLibraryRule_Test(bool hasLibrary)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(hasLibrary ? Library(("adult", "a")) : null);
        const string text = "a AND rule(\"missing\")";

        CompilationResult<RuleTestContext> result = compiler.Compile(text);

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.UnknownLibraryRule, diagnostic.Code);
        Assert.Contains("'missing'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(text.IndexOf("rule", StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.Equal(text.Length, diagnostic.Span.End);
    }

    /// <summary>
    /// A library rule whose predicate or data source the caller's compile does not hold fails the compile with one
    /// diagnostic that names the library rule and the missing name, located at the reference.
    /// </summary>
    /// <param name="libraryText">The text of the library rule.</param>
    /// <param name="missingName">The name the caller does not hold.</param>
    [Theory]
    [InlineData("a AND notRegistered", "notRegistered")]
    [InlineData("hasRole(role: from(\"user\", \"$.role\"))", "user")]
    public void Compile_LibraryRuleNameTheCallerDoesNotHold_ReportsInvalidLibraryRuleNamingIt_Test(
        string libraryText,
        string missingName
    )
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library(("adult", libraryText)));
        const string text = "c AND rule(\"adult\")";

        CompilationResult<RuleTestContext> result = compiler.Compile(text);

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidLibraryRule, diagnostic.Code);
        Assert.Contains("'adult'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(missingName, diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(text.IndexOf("rule", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>
    /// A library rule's data source names resolve against the caller's compile: the same library rule compiles when the
    /// caller declares the data source.
    /// </summary>
    [Fact]
    public void Compile_LibraryRuleDataSourceTheCallerDeclares_Compiles_Test()
    {
        DataSourceDeclarations dataSources = [];
        dataSources.Add("user");
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(
            Library(("admin", "hasRole(role: from(\"user\", \"$.role\"))")),
            dataSources
        );

        CompiledRule<RuleTestContext> rule = Compile(compiler, "rule(\"admin\")");

        Assert.Equal(Compile(compiler, "hasRole(role: from(\"user\", \"$.role\"))").CanonicalText, rule.CanonicalText);
    }

    /// <summary>A library rule that references another library rule expands both levels.</summary>
    [Fact]
    public void Compile_LibraryRuleReferencingAnotherLibraryRule_ExpandsBothLevels_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(
            Library(("adult", "a AND NOT b"), ("adult-customer", "rule(\"adult\") AND c"))
        );

        CompiledRule<RuleTestContext> rule = Compile(compiler, "rule(\"adult-customer\")");

        Assert.Equal(Compile(compiler, "(a AND NOT b) AND c").CanonicalText, rule.CanonicalText);
    }

    /// <summary>
    /// The words <c>rule</c> and <c>hash</c> match without case, like the keywords of rule text, and a library rule name
    /// matches without case.
    /// </summary>
    [Fact]
    public void Compile_ReferenceInAnyCase_ExpandsTheLibraryRule_Test()
    {
        RuleLibrary library = Library(("adult", "a AND b"));
        Assert.True(library.TryGet("adult", out LibraryRule? adult));
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(library);

        CompiledRule<RuleTestContext> rule = Compile(
            compiler,
            $"RULE(\"ADULT\") OR Rule(\"Adult\", HASH: \"{adult.Hash.ToLowerInvariant()}\")"
        );

        Assert.Equal(Compile(compiler, "(a AND b) OR (a AND b)").CanonicalText, rule.CanonicalText);
    }

    /// <summary>
    /// The word <c>rule</c> is not reserved: a predicate named <c>rule</c> is still a term, bare or with named arguments,
    /// because a library rule reference is the only form with a quoted string directly after the parenthesis.
    /// </summary>
    /// <param name="withArgument">Whether the predicate takes a named argument.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Compile_PredicateNamedRule_IsStillATerm_Test(bool withArgument)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        RuleCompiler<RuleTestContext> compiler = new(
            (withArgument ? builder.AddStringArgPredicate("rule", "name", "x") : builder.AddConstant("rule", true)).Build(),
            new CompilerOptions(Library: Library(("x", "FALSE")))
        );

        CompilationResult<RuleTestContext> result = compiler.Compile(withArgument ? "Rule(name: \"x\")" : "rule");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(withArgument ? "rule(name: \"x\")" : "rule", result.CompiledRule?.CanonicalText);
    }

    /// <summary>
    /// A reference inside a <c>let</c> definition expands, and a library rule's own <c>let</c> definitions are in scope
    /// in that library rule only.
    /// </summary>
    [Fact]
    public void Compile_ReferenceInADefinitionAndDefinitionsInALibraryRule_Expand_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library(("adult", "let both = a AND c; both OR b")));

        CompiledRule<RuleTestContext> rule = Compile(compiler, "let grown = rule(\"adult\"); grown AND c");

        Assert.Equal(Compile(compiler, "((a AND c) OR b) AND c").CanonicalText, rule.CanonicalText);
    }

    /// <summary>
    /// A deep chain of library rules, each referencing the one below it many times, is expanded once for each rule and
    /// not once for each path through the chain: the compile ends quickly with the node count limit, named against the
    /// library rule that first passes it, instead of running for an exponential time.
    /// </summary>
    [Fact(Timeout = 30_000)]
    public async Task Compile_HighFanoutChainOfLibraryRules_StopsAtTheNodeLimitQuickly_Test()
    {
        const int depth = 16;
        List<(string Name, string Text)> rules = [("level0", "a")];
        for (int level = 1; level <= depth; level++)
        {
            rules.Add(($"level{level}", string.Join(" AND ", Enumerable.Repeat($"rule(\"level{level - 1}\")", 4))));
        }

        RuleCompiler<RuleTestContext> compiler = CreateCompiler(Library([.. rules]));

        CompilationResult<RuleTestContext> result = await Task.Run(
            () => compiler.Compile($"rule(\"level{depth}\")"),
            TestContext.Current.CancellationToken
        );

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidLibraryRule, diagnostic.Code);
        Assert.Contains(DiagnosticCodes.MaxNodeCountExceeded, diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A <c>let</c> definition whose body holds a bad library reference, used many times, reports the error once: the
    /// uses share the one reference site, so the same code, span and message are not repeated for each use.
    /// </summary>
    /// <param name="reference">The bad library reference in the definition body.</param>
    /// <param name="code">The diagnostic code the reference gives.</param>
    [Theory]
    [InlineData("rule(\"missing\")", DiagnosticCodes.UnknownLibraryRule)]
    [InlineData(
        "rule(\"adult\", hash: \"0000000000000000000000000000000000000000000000000000000000000000\")",
        DiagnosticCodes.LibraryRuleHashMismatch
    )]
    [InlineData("rule(\"first\")", DiagnosticCodes.LibraryRuleCycleDetected)]
    public void Compile_DefinitionHoldingABadReferenceUsedManyTimes_ReportsTheErrorOnce_Test(string reference, string code)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(
            Library(("adult", "a AND b"), ("first", "rule(\"second\") AND a"), ("second", "b OR rule(\"first\")"))
        );

        CompilationResult<RuleTestContext> result = compiler.Compile($"let bad = {reference}; bad AND bad AND (bad OR c)");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(code, diagnostic.Code);
    }

    /// <summary>
    /// Inside a library rule every reference takes the site of the outermost reference, so an unpinned and a stale pinned
    /// reference to the same rule share a site: the stale pin is still reported after the unpinned reference expands.
    /// </summary>
    [Fact]
    public void Compile_UnpinnedAndStalePinnedReferenceInOneLibraryRule_ReportsTheStalePin_Test()
    {
        string stale = new('0', 64);
        RuleCompiler<RuleTestContext> compiler = CreateCompiler(
            Library(("adult", "a AND b"), ("outer", $"rule(\"adult\") OR rule(\"adult\", hash: \"{stale}\")"))
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("rule(\"outer\")");

        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.LibraryRuleHashMismatch, diagnostic.Code);
    }

    private static RuleLibrary Library(params (string Name, string Text)[] rules)
    {
        return new RuleLibrary(rules.Select(rule => new KeyValuePair<string, string>(rule.Name, rule.Text)));
    }

    private static CompiledRule<RuleTestContext> Compile(RuleCompiler<RuleTestContext> compiler, string text)
    {
        CompilationResult<RuleTestContext> result = compiler.Compile(text);
        return result.CompiledRule
            ?? throw new InvalidOperationException($"'{text}' did not compile: {result.FormatDiagnostics(text)}");
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler(
        RuleLibrary? library,
        DataSourceDeclarations? dataSources = null
    )
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", false)
                .AddConstant("c", true)
                .AddStringArgPredicate("hasRole", "role", "admin")
                .Build(),
            new CompilerOptions(DataSources: dataSources, Library: library)
        );
    }
}
