namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Ticket 11-2: <c>let</c> definitions and parameters in rule text (ADR-0008). Covers compile-time expansion, every
/// guardrail diagnostic, the opt-in unused-definition lint, and that printing, the analyzer and <c>RuleDiff</c> see
/// only the already-expanded tree.
/// </summary>
public sealed class DefinitionsTests
{
    /// <summary>A parameterless <c>let</c> use expands to the same compiled tree as writing its body out by hand.</summary>
    [Fact]
    public void Compile_ParameterlessDefinitionUse_ExpandsToHandWrittenEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> withDefinition = Compile(compiler, "let adult = a; adult AND b");
        CompiledRule<RuleTestContext> handWritten = Compile(compiler, "a AND b");

        Assert.False(RuleDiff.Compare(handWritten, withDefinition).HasChanges);
        Assert.Equal("a AND b", withDefinition.PrintRuleText());
    }

    /// <summary>
    /// A parameterized <c>let</c> use substitutes each argument (itself a full sub-expression) for the matching
    /// parameter, expanding to the same tree as writing the substituted body out by hand.
    /// </summary>
    [Fact]
    public void Compile_ParameterizedDefinitionUse_SubstitutesArgumentsAndExpands_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> withDefinition = Compile(compiler, "let bothTrue(x, y) = x AND y; bothTrue(a, b)");
        CompiledRule<RuleTestContext> handWritten = Compile(compiler, "a AND b");

        Assert.False(RuleDiff.Compare(handWritten, withDefinition).HasChanges);
    }

    /// <summary>A definition may itself use another definition; expansion threads through both.</summary>
    [Fact]
    public void Compile_DefinitionUsingAnotherDefinition_ExpandsBothLevels_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> withDefinitions = Compile(
            compiler,
            "let adult = a; let adultAndB = adult AND b; adultAndB"
        );
        CompiledRule<RuleTestContext> handWritten = Compile(compiler, "a AND b");

        Assert.False(RuleDiff.Compare(handWritten, withDefinitions).HasChanges);
    }

    /// <summary>A use may name a definition declared later in the document; declaration order does not matter.</summary>
    [Fact]
    public void Compile_UseBeforeDeclaration_StillExpands_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = Compile(compiler, "let first = second; let second = a; first");
        CompiledRule<RuleTestContext> handWritten = Compile(compiler, "a");

        Assert.False(RuleDiff.Compare(handWritten, rule).HasChanges);
    }

    /// <summary>A definition that references itself directly is a cycle, named in the diagnostic's message.</summary>
    [Fact]
    public void Compile_SelfReferencingDefinition_ReportsDefinitionCycleNamingIt_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let x = x; x");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionCycleDetected);
        Assert.Contains("x", diagnostic.Message);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A cycle through a chain of definitions is reported naming every definition in the cycle.</summary>
    [Fact]
    public void Compile_MutuallyReferencingDefinitions_ReportsDefinitionCycleNamingBoth_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let x = y; let y = x; x");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionCycleDetected);
        Assert.Contains("x", diagnostic.Message);
        Assert.Contains("y", diagnostic.Message);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A definition name declared twice in the same document is a name-collision error, not a silent override.</summary>
    [Fact]
    public void Compile_DuplicateDefinitionName_ReportsDefinitionNameCollision_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let dup = TRUE; let dup = FALSE; dup");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// A use of a name that collided with a registered predicate at declaration (so it was never added to the
    /// document's definitions) is reported as an unknown definition too, on top of the collision itself.
    /// </summary>
    [Fact]
    public void Compile_UseOfPredicateCollidedDefinitionName_AlsoReportsUnknownDefinition_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let a = TRUE; a");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownDefinition);
    }

    /// <summary>A definition named after a reserved word is a name collision, independent of whether it is ever used.</summary>
    [Fact]
    public void Compile_DefinitionNamedAfterReservedWord_ReportsDefinitionNameCollision_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let True = a; b");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
    }

    /// <summary>A definition named after a predicate registered with the compiler is a name collision.</summary>
    [Fact]
    public void Compile_DefinitionNamedAfterRegisteredPredicate_ReportsDefinitionNameCollision_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let a = TRUE; b");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DefinitionNameCollision);
    }

    /// <summary>A use supplying the wrong number of arguments is a distinct compile error naming the counts.</summary>
    [Fact]
    public void Compile_UseWithWrongArgumentCount_ReportsDefinitionArgumentCountMismatch_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let bothTrue(x, y) = x AND y; bothTrue(a)");

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.DefinitionArgumentCountMismatch
        );
        Assert.Equal("2 argument(s)", diagnostic.Expected);
        Assert.Equal("1 argument(s)", diagnostic.Found);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// A definition whose parameter list repeats a name (case-insensitively) is a compile error (ticket 20), not a
    /// silent binding where <c>ExpandUse</c>'s case-insensitive parameter dictionary lets the later parameter
    /// overwrite the earlier one's argument.
    /// </summary>
    [Fact]
    public void Compile_DefinitionWithDuplicateParameterName_ReportsDuplicateDefinitionParameter_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let both(x, X) = x AND X; both(a, b)");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.DuplicateDefinitionParameter);
        Assert.Contains("both", diagnostic.Message);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// The exact ticket 20 failure scenario: without the fix, <c>both(isOwner, isEditor)</c> compiled silently to
    /// <c>isEditor AND isEditor</c>, dropping <c>isOwner</c>, so a rule that should evaluate <c>False</c> (isOwner is
    /// False) instead evaluated <c>True</c>. The fix rejects the definition at compile time, so the rule never
    /// reaches evaluation with the wrong meaning.
    /// </summary>
    [Fact]
    public void Compile_DuplicateParameterDefinitionUsedWithDifferingArguments_FailsCompilationInsteadOfDroppingAnArgument_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("isOwner", false)
                .AddConstant("isEditor", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("let both(x, X) = x AND X; both(isOwner, isEditor)");

        Assert.Null(result.CompiledRule);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DuplicateDefinitionParameter);
    }

    /// <summary>A JSON definition entry whose <c>parameters</c> repeats a name (case-insensitively) is the same compile error as the rule-text form.</summary>
    [Fact]
    public void CompileJson_DefinitionWithDuplicateParameterName_ReportsDuplicateDefinitionParameter_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string json = """
            {
              "definitions": {
                "both": {
                  "parameters": ["x", "X"],
                  "body": {
                    "op": "and",
                    "operands": [{ "predicate": "x" }, { "predicate": "X" }]
                  }
                }
              },
              "op": "use",
              "name": "both",
              "operands": [{ "predicate": "a" }, { "predicate": "b" }]
            }
            """;

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DuplicateDefinitionParameter);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A YAML definition entry whose <c>parameters</c> repeats a name (case-insensitively) is the same compile error as the rule-text form.</summary>
    [Fact]
    public void CompileYaml_DefinitionWithDuplicateParameterName_ReportsDuplicateDefinitionParameter_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string yaml = """
            definitions:
              both:
                parameters: [x, X]
                body:
                  op: and
                  operands:
                    - predicate: x
                    - predicate: X
            op: use
            name: both
            operands:
              - predicate: a
              - predicate: b
            """;

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(yaml);

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.DuplicateDefinitionParameter);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// A non-cyclic chain of uses deeper than <see cref="CompilerOptions.MaxDefinitionDepth"/> is a distinct compile
    /// error, independent of <c>MaxDepth</c> and the cycle check.
    /// </summary>
    [Fact]
    public void Compile_DefinitionChainDeeperThanMaxDefinitionDepth_ReportsMaxDefinitionDepthExceeded_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        string chain = string.Join(" ", Enumerable.Range(0, 10).Select(i => i == 0 ? "let d0 = a;" : $"let d{i} = d{i - 1};"));

        CompilationResult<RuleTestContext> result = compiler.Compile($"{chain} d9");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDefinitionDepthExceeded);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>A chain within the default <see cref="CompilerOptions.MaxDefinitionDepth"/> of 8 compiles cleanly.</summary>
    [Fact]
    public void Compile_DefinitionChainWithinMaxDefinitionDepth_CompilesWithoutError_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        string chain = string.Join(" ", Enumerable.Range(0, 5).Select(i => i == 0 ? "let d0 = a;" : $"let d{i} = d{i - 1};"));

        CompilationResult<RuleTestContext> result = compiler.Compile($"{chain} d4");

        Assert.NotNull(result.CompiledRule);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDefinitionDepthExceeded);
    }

    /// <summary>Expanding a use that would overrun <see cref="CompilerOptions.MaxNodeCount"/> reports the normal node-count error, with no special-casing for where the nodes came from.</summary>
    [Fact]
    public void Compile_UseExpandingPastMaxNodeCount_ReportsMaxNodeCountExceeded_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build(),
            new CompilerOptions(MaxNodeCount: 5)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("let big(x) = x AND x AND x AND x AND x AND x; big(a)");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// A chain of definitions only 6 levels deep (well within the default <see cref="CompilerOptions.MaxDefinitionDepth"/>
    /// of 8), where each level ANDs 12 uses of the one below, expands to millions of nodes if fully materialized
    /// (12^6, about 3 million). Compiling it reports <see cref="DiagnosticCodes.MaxNodeCountExceeded"/> quickly
    /// instead of allocating the whole expansion before <c>RuleNodeCompiler.Build</c>'s own count ever runs
    /// (ticket 04).
    /// </summary>
    [Fact]
    public void Compile_ShallowHighFanoutDefinitionChain_ReportsMaxNodeCountInsteadOfExponentialBlowup_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build(),
            new CompilerOptions(MaxNodeCount: 50)
        );
        string chain = BuildFanoutChain(depth: 6, fanout: 12);

        CompilationResult<RuleTestContext> result = compiler.Compile(chain);

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// The same shallow, high-fanout blow-up as above, read through <see cref="RuleDefinitions.ReadText(string, CompilerOptions?)"/>
    /// with no options supplied: the default <see cref="CompilerOptions.MaxNodeCount"/> of 512 is still far below the
    /// roughly 3 million nodes the chain would expand to, so the walk that maps compiled nodes back to their sources
    /// must be bounded too, not just <c>DefinitionExpander</c> (ticket 04, finding F3.1).
    /// </summary>
    [Fact]
    public void ReadText_ShallowHighFanoutDefinitionChain_IsBoundedByDefaultMaxNodeCount_Test()
    {
        string chain = BuildFanoutChain(depth: 6, fanout: 12);

        RuleDefinitions read = RuleDefinitions.ReadText(chain);

        Assert.Contains(read.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
    }

    /// <summary>An unused definition is silent by default.</summary>
    [Fact]
    public void Compile_UnusedDefinitionWithoutLint_RaisesNoDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("let unused = a; b");

        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.UnusedDefinition);
        Assert.NotNull(result.CompiledRule);
    }

    /// <summary>Opting into the <see cref="LintRules.UnusedDefinition"/> lint reports a declared-but-never-used definition.</summary>
    [Fact]
    public void Compile_UnusedDefinitionWithLintEnabled_ReportsUnusedDefinition_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(Lints: LintRules.UnusedDefinition)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("let unused = a; b");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UnusedDefinition);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("unused", diagnostic.Message);
    }

    /// <summary>A used definition is not reported by the unused-definition lint.</summary>
    [Fact]
    public void Compile_UsedDefinitionWithLintEnabled_RaisesNoUnusedDefinitionDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build(),
            new CompilerOptions(Lints: LintRules.UnusedDefinition)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("let used = a; used");

        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.UnusedDefinition);
    }

    /// <summary>A JSON document's <c>definitions</c> section compiles to the same tree as the rule-text equivalent, including a nested definition using another one.</summary>
    [Fact]
    public void CompileJson_DefinitionsWithNestedUse_EqualsRuleTextEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string json = """
            {
              "definitions": {
                "adult": { "body": { "predicate": "a" } },
                "adultAndB": {
                  "body": {
                    "op": "and",
                    "operands": [
                      { "op": "use", "name": "adult" },
                      { "predicate": "b" }
                    ]
                  }
                }
              },
              "op": "use",
              "name": "adultAndB"
            }
            """;

        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(json).CompiledRule!;
        CompiledRule<RuleTestContext> fromText = Compile(compiler, "let adult = a; let adultAndB = adult AND b; adultAndB");

        Assert.Equal(fromText.CanonicalText, fromJson.CanonicalText);
        Assert.Equal("a AND b", fromJson.CanonicalText);
    }

    /// <summary>A JSON <c>use</c> node with parameters substitutes its arguments, matching the rule-text equivalent.</summary>
    [Fact]
    public void CompileJson_ParameterizedDefinitionUse_EqualsRuleTextEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string json = """
            {
              "definitions": {
                "bothTrue": {
                  "parameters": ["x", "y"],
                  "body": {
                    "op": "and",
                    "operands": [{ "predicate": "x" }, { "predicate": "y" }]
                  }
                }
              },
              "op": "use",
              "name": "bothTrue",
              "operands": [{ "predicate": "a" }, { "predicate": "b" }]
            }
            """;

        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(json).CompiledRule!;
        CompiledRule<RuleTestContext> fromText = Compile(compiler, "let bothTrue(x, y) = x AND y; bothTrue(a, b)");

        Assert.Equal(fromText.CanonicalText, fromJson.CanonicalText);
    }

    /// <summary>
    /// A JSON use of an undeclared definition is reported at the use's own 'name' path, which RuleCompiler resolves to a
    /// real span in the JSON text (ADR-0008 decision 8), not left unlocated.
    /// </summary>
    [Fact]
    public void CompileJson_UseOfUnknownDefinition_ReportsUnknownDefinitionLocatedAtNamePath_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string json = """{"op": "use", "name": "nope"}""";

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownDefinition);
        Assert.Equal("$.name", diagnostic.Path);
        Assert.NotEqual(SourceSpan.None, diagnostic.Span);
        Assert.Equal(json.IndexOf("\"nope\"", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>A YAML document's <c>definitions</c> section compiles to the same tree as the rule-text equivalent.</summary>
    [Fact]
    public void CompileYaml_ParameterlessDefinitionUse_EqualsRuleTextEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string yaml = """
            definitions:
              adult:
                body:
                  predicate: a
            op: use
            name: adult
            """;

        CompiledRule<RuleTestContext> fromYaml = compiler.CompileYaml(yaml).CompiledRule!;
        CompiledRule<RuleTestContext> fromText = Compile(compiler, "let adult = a; adult");

        Assert.Equal(fromText.CanonicalText, fromYaml.CanonicalText);
    }

    /// <summary>
    /// A YAML use of an undeclared definition is reported with a real span (YamlDotNet keeps node positions, so no path
    /// resolution pass is needed), located at the 'name' key's own value.
    /// </summary>
    [Fact]
    public void CompileYaml_UseOfUnknownDefinition_ReportsUnknownDefinitionWithRealSpan_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string yaml = "op: use\nname: nope\n";

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(yaml);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownDefinition);
        Assert.NotEqual(SourceSpan.None, diagnostic.Span);
        Assert.Equal(yaml.IndexOf("nope", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    private static CompiledRule<RuleTestContext> Compile(RuleCompiler<RuleTestContext> compiler, string text)
    {
        return compiler.Compile(text).CompiledRule ?? throw new InvalidOperationException($"'{text}' did not compile.");
    }

    /// <summary>
    /// Builds <c>let</c> text for a chain of <paramref name="depth"/> definitions, each ANDing <paramref name="fanout"/>
    /// uses of the one before it, so the fully expanded tree has roughly <paramref name="fanout"/>^<paramref name="depth"/>
    /// nodes even though the chain itself is only <paramref name="depth"/> levels deep.
    /// </summary>
    private static string BuildFanoutChain(int depth, int fanout)
    {
        System.Text.StringBuilder text = new();
        text.Append("let d0 = a;");
        for (int level = 1; level <= depth; level++)
        {
            string body = string.Join(" AND ", Enumerable.Repeat($"d{level - 1}", fanout));
            text.Append($" let d{level} = {body};");
        }

        text.Append($" d{depth}");
        return text.ToString();
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096)
        );
    }
}
