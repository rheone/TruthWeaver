using System.Runtime.CompilerServices;

// TruthWeaver.Yaml reuses this package's raw parse-tree types (RuleNode and its
// variants, RawLiteral) and RuleCompiler.CompileFromNode so the YAML front end shares the exact same
// validate/analyze/build pipeline as the DSL and JSON front ends, rather than re-implementing it.
[assembly: InternalsVisibleTo("TruthWeaver.Yaml")]

// TruthWeaver.Tests needs direct access to internal units (Lexer, DslParser, Analyzer,
// BddManager, etc.) so unit tests aren't forced through the public RuleCompiler pipeline.
[assembly: InternalsVisibleTo("TruthWeaver.Tests")]

// TruthWeaver.Testing throws the internal TestDoubleMisuseException when a test double is misused (an exhausted
// FakePredicates.Scripted script). The evaluator lets only that type escape, and keeping it internal means no
// production predicate can throw it, so every other predicate exception is still a fault (ADR-0001).
[assembly: InternalsVisibleTo("TruthWeaver.Testing")]

// TruthWeaver.Benchmarks measures the Parse/Validate+Build/Analyze/Lint
// pipeline stages individually (RuleNodeCompiler, Analyzer, Linter), which RuleCompiler otherwise
// runs as one opaque call, so compile-cost regressions can be attributed to a stage.
[assembly: InternalsVisibleTo("TruthWeaver.Benchmarks")]
