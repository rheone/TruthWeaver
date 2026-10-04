# TruthWeaver

[![CI](https://github.com/rheone/TruthWeaver/actions/workflows/ci.yml/badge.svg)](https://github.com/rheone/TruthWeaver/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](global.json)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

A general-purpose **Strong Kleene (K3)** expression engine for .NET. Every
expression evaluates to one of three values — `True`, `False` or `Unknown` — and
`Unknown` is never silently turned into `True` or `False`. Author a rule once as
text, compile it into an immutable tree, and evaluate it many times against
whatever application context you supply — a user, a request, a resource, or
anything else.

<details>
<summary><strong>Table of contents</strong></summary>

- [What it is (and isn't)](#what-it-is-and-isnt)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [Packages](#packages)
- [Architecture](#architecture)
- [Features](#features)
- [Writing rules](#writing-rules)
- [Rewriting and equivalence](#rewriting-and-equivalence)
- [Predicate types](#predicate-types)
- [Examples](#examples)
- [Reading diagnostics](#reading-diagnostics)
  - [JSON and YAML rules](#json-and-yaml-rules)
- [Benchmarks](#benchmarks)
- [Glossary](#glossary)
- [Appendix: Truth tables](#appendix-truth-tables)
- [Design documents](#design-documents)
- [License](#license)

</details>

## What it is (and isn't)

`TruthWeaver` answers one question: *what is the truth value of this expression
right now, for this context?* — `True`, `False` or `Unknown`. It knows about `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`,
`PARITY`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `If`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`, `ExactlyOne`, the threshold family (`AtLeast`/`AtMost`/`GreaterThan`/
`LessThan`/`Exactly`), terms, and evaluation. It does not know about
permissions, workflows, or policies — those are things you build *on top* of
it. A permission check ("can the current user do X") is one consumer of this
engine, not what the engine itself is.

| Concept | Meaning |
| --- | --- |
| **Rule** | A named unit of persistence: metadata + one expression. |
| **Expression** | The three-valued tree — operators over terms, constants and sub-expressions. |
| **Predicate** | A registered, reusable implementation, e.g. `hasTopping`, `lovesPineapple`. |
| **Term** | A predicate bound to concrete arguments, e.g. `hasTopping(topping: "greenOlives")` — the tree's leaf node. |
| **Operator** | `AND` `OR` `NOT` `XOR` `EQUIVALENT` `IMPLIES` `NAND` `NOR` `PARITY` `ANY` `ALL` `NONE` `BETWEEN(min, max)` `COALESCE` `If` `IsTrue` `IsFalse` `IsUnknown` `IsKnown` `ExactlyOne` and the threshold family (`AtLeast(k)`/`AtMost(k)`/`GreaterThan(k)`/`LessThan(k)`/`Exactly(k)`), plus the constants `True`/`False`/`Unknown`. Operators are case-insensitive and most have a symbol spelling (`&&`, `||`, `!`, `∧`, `∨`, `¬`, `⊕`, `→`, `↔`, `↑`, `↓`, `??`, `? :`). `Project` and `Collapse` are not part of the rule language: they are methods on the result (`Decision.Project(unknownAs)` and `Decision.Collapse(policy)`, see [Collapse](docs/strong-k3/result-transformations/collapse.md)); inside a rule use `COALESCE(x, True)` / `COALESCE(x, False)`. See [Operations](docs/strong-k3/specification/operations.md). |
| **Decision** | The evaluation result: a `TruthValue` plus any faults, and optionally a trace. `IsSatisfied` is fail-closed: only `True` is satisfied. |

Full vocabulary and the predicate-author contract: [CONTEXT.md](CONTEXT.md).

Release status, version policy and the migration guide for every breaking change: [CHANGELOG.md](CHANGELOG.md).

## Requirements

Building from source needs at least the SDK version floor in
[`global.json`](global.json) — currently `11.0.100-rc.1.26425.128` — but
`rollForward: latestMajor` with `allowPrerelease: true` means any later
major .NET SDK on the machine, preview or RC included, is accepted. This
isn't a pin to that exact patch.

## Getting started

1. **Reference the packages you need.** A service that only *implements*
   predicates references `TruthWeaver.Abstractions`; a host that
   authors and evaluates rules references `TruthWeaver` (and
   `TruthWeaver.Yaml` if it wants YAML too). See
   [Packages](#packages) below.

   ```xml
   <ProjectReference Include="..\TruthWeaver\TruthWeaver.csproj" />
   ```

2. **Implement a predicate.** A zero-argument predicate is the simplest
   shape — a class implementing `IPredicate<TContext>`. A predicate answers
   a three-valued `TruthValue`: return `TruthValue.Unknown` when the answer
   is legitimately indeterminate (that is a normal result and records no
   fault), and throw only for a genuine failure:

   ```csharp
   public sealed class LovesPineapple : IPredicate<Customer>
   {
       public static PredicateSchema Schema =>
           PredicateSchema.NoArguments("lovesPineapple", "Loves Pineapple", "Does this customer like pineapple on pizza?");

       public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct) =>
           ValueTask.FromResult(customer.LovesPineapple ? TruthValue.True : TruthValue.False);
   }
   ```

3. **Register it and compile a rule:**

   ```csharp
   PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder().Add<LovesPineapple>().Build();
   RuleCompiler<Customer> compiler = new(registry);
   CompilationResult<Customer> result = compiler.Compile("lovesPineapple");

   if (!result.Succeeded)
   {
       // result.Diagnostics explains why - surface it to whoever authored the rule.
       return;
   }
   ```

4. **Evaluate it against a context:**

   ```csharp
   Decision decision = await result.CompiledRule!.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);

   if (decision.IsSatisfied)
   {
       // allowed
   }
   ```

That's the whole lifecycle: implement → register → compile once → evaluate
many times. [Architecture](docs/architecture.md) maps
that lifecycle onto the actual folders, and [Examples](docs/examples.md) builds up
from here to named arguments, the full operator set, and the full ADR-0003
worked example in DSL, JSON, and YAML.

## Packages

| Package | Use it to |
| --- | --- |
| `TruthWeaver.Abstractions` | Implement predicates. It has no third-party dependency. |
| `TruthWeaver` | Parse, compile, analyze and evaluate rules. |
| `TruthWeaver.Yaml` | Read and write rules and data sources as YAML. |
| `TruthWeaver.DataSources.Json` | Read variable values from a JSON document. |
| `TruthWeaver.Predicates` | Use ready-made predicates for common checks. |
| `TruthWeaver.Testing` | Assert on decisions and fake predicates in tests. |

A service that only implements predicates references `TruthWeaver.Abstractions` alone. For the dependencies and the contents of each package, see [Packages](docs/packages.md).

## Architecture

The source layout, the compilation pipeline and the evaluation flow are in [Architecture](docs/architecture.md). The evaluation behavior is in [Evaluation](docs/strong-k3/specification/evaluation.md).

## Features

- **Kleene three-valued logic.** Every operator follows the three-valued
  truth tables in [ADR-0001](docs/adr/0001-kleene-failure-model.md) (full
  tables: [Appendix](#appendix-truth-tables)) — a predicate fault becomes
  `Unknown`, never a thrown exception or a silently coerced `false`, and a
  predicate can also answer `Unknown` directly. Entry
  point: [`Evaluator`](src/TruthWeaver/Evaluation/Evaluator.cs).
- **A complete Strong Kleene (K3) language, plus external operators.** The
  Strong Kleene connectives are `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`,
  `XOR`, `PARITY`, `NAND`, `NOR`, the cardinality operators
  (`AtLeast`/`AtMost`/`Exactly`, `ANY`/`ALL`/`NONE`/`BETWEEN`) and `If`/`? :`.
  `COALESCE`/`??` and the four inspections (`IsTrue`, `IsFalse`, `IsUnknown`,
  `IsKnown`) are external operators, not K3 connectives. `Unknown` is also a
  constant. Operators are case-insensitive, have symbol spellings, and every
  notation compiles to the same tree with one canonical form. See
  [Operations](docs/strong-k3/specification/operations.md) and
  [Strong Kleene connectives and external operators](docs/strong-k3/specification/semantics.md#strong-kleene-connectives-and-external-operators).
- **Explicit boundaries.** `COALESCE(x, True|False)` resolves `Unknown` anywhere
  inside a rule; `Decision.Project(unknownAs)` and `Decision.Collapse(policy)` turn
  the rule's three-valued result into a definite value or a two-valued answer at the
  call site, and are not part of the rule. See
  [Result transformations](docs/strong-k3/result-transformations/README.md).
- **Rule rewriting.** Opt-in, value-preserving transforms return a new rule:
  expand to primitives, to NAND-only or NOR-only, compress back to derived
  operators, canonicalize, simplify, plus whitespace normalization. See
  [Rewriting rules](docs/rewriting-rules.md#rewriting-rules).
- **Readable diagnostics.** Every authoring error is a structured `Diagnostic`
  (code, span or JSON/YAML path, expected/found, "did you mean") with a
  plain-text formatter. See [Reading diagnostics](#reading-diagnostics).
- **Per-evaluation memoization.** A term referenced from multiple branches
  of the same rule is invoked at most once per evaluation, keyed by
  structural term identity (see [CONTEXT.md#term-identity](CONTEXT.md#term-identity)).
  Entry point: [`Evaluator`](src/TruthWeaver/Evaluation/Evaluator.cs).
- **A Strong K3 BDD analyzer**, not brute-force truth tables, flags
  sub-expressions that are `True` (or `False`) for every `{True, False, Unknown}`
  assignment of their terms (e.g.
  `hasTopping(topping: "greenOlives") AND FALSE`) as compile diagnostics. It does
  not flag `A AND NOT A` or `A OR NOT A`: both are `Unknown` when `A` is.
  Entry point: [`Analyzer`](src/TruthWeaver/Analysis/Analyzer.cs) and
  [`BddManager`](src/TruthWeaver/Analysis/BddManager.cs).
- **Resource limits and `CompilationMode.Lenient`.** `CompilerOptions`
  bounds tree depth, node count, and the analyzer's term cap so an
  admin-authored rule can't hang a request thread; `Lenient` mode compiles
  an unregistered predicate to a permanent `Unknown` term instead of an
  error. Entry point: [`CompilerOptions`](src/TruthWeaver/Compilation/CompilerOptions.cs).
- **`EvaluationOptions`**: an opt-in `FaultBudget` for fail-fast behavior
  during a known outage, an `Exhaustive` mode that runs every reachable term
  without changing the result, and an overall evaluation timeout linked into
  the caller's `CancellationToken`. Entry point:
  [`EvaluationOptions`](src/TruthWeaver/Evaluation/EvaluationOptions.cs).
- **Scoped DI resolution.** Class-based predicates resolve fresh from the
  `IServiceProvider` supplied to each evaluation call, so a predicate with a
  scoped dependency works correctly even though a `CompiledRule<TContext>`
  is long-lived and shared. Entry point:
  [`TruthWeaverServiceCollectionExtensions`](src/TruthWeaver/DependencyInjection/TruthWeaverServiceCollectionExtensions.cs).
- **Structured logging and metrics.** Faults, compile diagnostics, and
  rule-swap notifications log as structured events through `ILogger<T>`; a
  `"TruthWeaver"` `Meter` exposes counters for evaluations, faults, and
  compile diagnostics, observable through OpenTelemetry's `AddMeter` with no
  new dependency. Entry points: [`src/TruthWeaver/Logging`](src/TruthWeaver/Logging)
  and [`TruthWeaverMetrics`](src/TruthWeaver/Metrics/TruthWeaverMetrics.cs).
- **Structural rule diffing.** `RuleDiff.Compare` compares two compiled
  rules and reports which operator, term, or constant nodes were added,
  removed, or changed, each located by operand-index path and paired with a
  human-readable description — useful for "what did this edit actually
  change" tooling. The result also says whether the change preserves meaning
  (see [Rule equivalence](docs/rewriting-rules.md#rule-equivalence)). Entry point:
  [`RuleDiff`](src/TruthWeaver/Diffing/RuleDiff.cs).
- **Strong K3 rule equivalence.** `RuleEquivalence.Compare` says whether two
  compiled rules give the same result for every `True`/`False`/`Unknown`
  assignment of their terms, with a counter-example when they do not. See
  [Rule equivalence](docs/rewriting-rules.md#rule-equivalence). Entry point:
  [`RuleEquivalence`](src/TruthWeaver/Analysis/RuleEquivalence.cs).
- **Diagram rendering.** A compiled rule renders as a Mermaid flowchart or
  an indented plain-text tree, optionally colored by one evaluation's
  result and short-circuit path — see
  [Rendering a rule as a diagram](docs/rulebuilder.md#rendering-a-rule-as-a-diagram). Entry
  points: [`MermaidTreePrinter`](src/TruthWeaver/Printing/MermaidTreePrinter.cs),
  [`PlainTextTreePrinter`](src/TruthWeaver/Printing/PlainTextTreePrinter.cs).
- **Ready-made predicates.** `TruthWeaver.Predicates` ships generic
  string-comparison, null/empty, set-equality, and regex-matching predicate
  factories so common checks don't need a hand-written class. Entry point:
  [`src/TruthWeaver.Predicates`](src/TruthWeaver.Predicates).
- **Test support.** `TruthWeaver.Testing` ships fluent `Decision`
  assertions and fake/scripted predicate factories (fixed answer, simulated
  fault, sequenced answers, `Unknown` answered directly) for testing without a hand-written
  `IPredicate<TContext>` per test. Entry point:
  [`src/TruthWeaver.Testing`](src/TruthWeaver.Testing).

## Writing rules

Rule text, JSON, YAML and `RuleBuilder` all compile to the same immutable tree.

- [Rule text](docs/rule-text.md): the grammar, grouping delimiters, whitespace and case.
- [Rule formats](docs/rule-formats.md): how to choose between rule text, JSON and YAML, and how to convert between them.
- [RuleBuilder, outlines and diagrams](docs/rulebuilder.md): assemble a rule in code, describe it and draw it.
- [Strong Kleene (K3) reference](docs/strong-k3/README.md): the meaning of every operator. See [Syntax](docs/strong-k3/specification/syntax.md) for spellings, precedence and operand counts, [Operations](docs/strong-k3/specification/operations.md) for the operator list, [Semantics](docs/strong-k3/specification/semantics.md#strong-kleene-connectives-and-external-operators) for connectives and external operators, and [Result transformations](docs/strong-k3/result-transformations/README.md) for `Collapse` and `Project`.

## Rewriting and equivalence

Rewrites return a new rule with the same value for every `True`/`False`/`Unknown` assignment, and equivalence checking compares two rules exactly. See [Rewriting rules and rule equivalence](docs/rewriting-rules.md) for `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`, `Simplify` and `RuleEquivalence`.

## Predicate types

Every predicate is a stateless lambda or a class that DI resolves, with zero, one or several rule-authored arguments. See [Predicate types](docs/predicates.md) for the four registration shapes, the ready-made predicates in `TruthWeaver.Predicates`, the externally selected value pattern and arguments read from a data source.

## Examples

Seven worked examples build up from a single predicate to the full rule in text, JSON, YAML and `RuleBuilder`, followed by a bonus on explaining a denied decision. See [Examples](docs/examples.md).

## Reading diagnostics

A rule that does not compile never throws; `Compile` returns a
`CompilationResult<TContext>` whose `Diagnostics` explain what is wrong. Each
`Diagnostic` is structured data first and text second, so an editor can lay it
out itself and a log can print it as is.

| Member          | Meaning                                                                                              |
| --------------- | ---------------------------------------------------------------------------------------------------- |
| `Code`          | Stable identifier such as `TRE0001` (see `DiagnosticCodes`).                                         |
| `Severity`      | `Error` blocks compilation; `Warning` and `Info` do not.                                             |
| `Message`       | A plain-language explanation of the problem.                                                         |
| `Span`          | Where it is in the rule text (0-based offset and length). `Span.GetLocation(source)` gives line and column. |
| `Path`          | Where it is in a JSON or YAML rule, for example `$.operands[1].op`; `null` for DSL text.             |
| `Expected` / `Found` | What the compiler needed and what it saw (`')'` and `']'`, `2 operands` and `3 operands`), when that applies. |
| `Suggestion`    | A `DiagnosticSuggestion`: a `Replacement` ("did you mean `AND`?") or a `Hint` (advice such as adding parentheses). |

```csharp
const string source = "a ANDD b";
CompilationResult<MyContext> result = compiler.Compile(source);

foreach (Diagnostic d in result.Diagnostics)
{
    SourceLocation at = d.Span.GetLocation(source);          // line 1, column 3
    Console.WriteLine($"{d.Code} {at.Line}:{at.Column} {d.Message}");
    Console.WriteLine($"expected {d.Expected}, found {d.Found}, try {d.Suggestion?.Text}");
}

// Or render everything as plain text for a log or an editor panel.
Console.WriteLine(result.FormatDiagnostics(source));
```

<!-- doctest:diagnostics-dsl a ANDD b -->
```text
TRE0001 error at line 1, column 3: Unexpected token 'ANDD' after end of expression.
  a ANDD b
    ^^^^
  Expected: an operator or the end of the rule
  Found: 'ANDD'
  Did you mean: AND
```

`DiagnosticFormatter.Format(diagnostic, source)` renders a single diagnostic;
without the source text the header shows `at offset 2` and the source line is
left out.

"Did you mean" suggestions come from a small, deterministic edit distance
(case-insensitive, counting a swapped pair of letters as one edit, with a
cut-off that scales with the length of the word) over the operators, aliases,
reserved words and the predicate names registered in your registry; equally
close candidates resolve to the ordinally first one, so the same typo always
gets the same answer. A word that is nowhere near anything known gets no
suggestion rather than a bad guess. They cover unknown predicate and operator
names, undeclared predicate argument names, and a
lone `&` or `|`.

### Lint rules (opt-in)

Beyond the Strong K3 tautology and contradiction warnings, the compiler can flag constructs that are redundant under
Strong K3 and say what to write instead. The lints are off by default, so a rule that compiled clean before keeps
compiling clean; switch them on with `CompilerOptions.Lints`:

```csharp
RuleCompiler<MyContext> compiler = new(registry, new CompilerOptions(Lints: LintRules.All));
CompilationResult<MyContext> result = compiler.Compile("NOT NOT isAdmin");
// TRE0023 info: a negation of a negation cancels out ... Did you mean: isAdmin
```

Each finding is an `Info` diagnostic (it never blocks compilation) with a `Replacement` suggestion holding the simpler
rule text, and a message that gives the Strong K3 reason the replacement means the same. Findings carry no source span
(the compiled tree does not remember where a node was written), so the message and `Found` show the construct. Every
suggestion is checked in the test suite with `RuleEquivalence`: it is the same rule for every `True`/`False`/`Unknown`
input. A lint never fires on a two-valued intuition that Strong K3 does not share (`a XOR a`, `a AND NOT a` and a
repeated `PARITY` operand are left alone).

| `LintRules` flag | Code | Flags | Suggests |
| --- | --- | --- | --- |
| `RedundantInspection` | `TRE0017` | `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` over an operand that can never be `Unknown`, or never be known (`IsKnown(IsTrue(a))`) | `True`/`False`, the operand, or its negation |
| `RedundantCoalesce` | `TRE0018` | a `COALESCE` operand that can never be `Unknown`, so the operands after it are unreachable | the operands up to and including it |
| `ConstantIfCondition` | `TRE0019` | an `If` whose condition is always `True` or always `False` | the branch that is always chosen |
| `IdenticalIfBranches` | `TRE0020` | `If(c, t, t)` | `t` |
| `VacuousCardinality` | `TRE0021` | a threshold or `BETWEEN` whose constant operands already fix the result (`AtLeast(1, a, TRUE)`) | the constant |
| `DuplicateOperands` | `TRE0022` | a structurally identical operand repeated inside `AND`, `OR`, `ANY`, `ALL` or `COALESCE` | the operator with each operand once |
| `DoubleNegation` | `TRE0023` | `NOT NOT x` | `x` |

`LintRules` is a flags enum: combine the ones you want (`LintRules.DuplicateOperands | LintRules.DoubleNegation`) or use
`LintRules.All`. The semantic lints (`TRE0017` to `TRE0019`, `TRE0021`) use the analyzer's BDD and are skipped for a
sub-expression with more than `CompilerOptions.MaxAnalysisTerms` distinct terms; the structural ones (`TRE0020`,
`TRE0022`, `TRE0023`) always run.

### JSON and YAML rules

A malformed JSON or YAML rule is located by `Path` instead of by line and
column: the route from the document root to the offending key, written the
same way for both formats (`$` is the root, `.name` a key, `[n]` a 0-based
sequence item). A YAML diagnostic also carries the `Span` of the offending node,
so `FormatDiagnostics(yaml)` adds the line and column and the source line; a JSON
diagnostic carries the span of the offending node in the same way (found by
re-reading the text, since `System.Text.Json` keeps no positions), and invalid JSON
syntax carries the parser's position.

```csharp
const string json = """{"op":"and","operands":[{"const":true},{"op":"orr","operands":[]}]}""";
Console.WriteLine(compiler.CompileJson(json).FormatDiagnostics(json));
```

<!-- doctest:diagnostics-json {"op":"and","operands":[{"const":true},{"op":"orr","operands":[]}]} -->
```text
TRE0014 error at $.operands[1].op (line 1, column 46): Unknown operator 'orr'.
  {"op":"and","operands":[{"const":true},{"op":"orr","operands":[]}]}
                                               ^^^^^
  Expected: a known operator
  Found: 'orr'
  Did you mean: or
```

Unknown `op` names are answered with the nearest operator in the tree's own
spelling (`atLeast`, not `AtLeast`) and unknown predicate names with the nearest
registered predicate. Where a field is wrong the path points at the field
(`$.k`, `$.policy`, `$.unknownAs`, `$.min`, `$.args.role`, `$.predicate`, `$.op`,
`$.const`); a wrong operand count points at `.operands`; a missing key is
reported at the node that should have held it. Invalid JSON or YAML syntax
reports the nearest valid ancestor (the innermost object or array still open) and
the parser's position:

<!-- doctest:diagnostics-json {"op":"and","operands":[{"const":true}, -->
```text
TRE0014 error at $.operands (line 1, column 39): Malformed JSON: Expected start of a property name or value, but instead reached end of data. LineNumber: 0 | BytePositionInLine: 38.
  {"op":"and","operands":[{"const":true},
                                        ^
  Expected: well-formed JSON
  Found: Expected start of a property name or value, but instead reached end of data. LineNumber: 0 | BytePositionInLine: 38.
```

The classes of malformed rule text each report as follows.

| Problem | Code | Expected / found | Suggestion |
| ------- | ---- | ---------------- | ---------- |
| Unknown predicate or operator name | `TRE0002` | a registered name or an operator / the name | nearest known name |
| Misspelt operator between operands, trailing tokens | `TRE0001` | an operator or the end of the rule / the token | nearest word operator |
| Missing operand or literal | `TRE0001` | a term, constant or `(` (or a literal) / the token or end of rule | none |
| Mismatched, unclosed or unmatched delimiter | `TRE0001` | the closer / the token or end of rule (an unclosed group is reported at its opener) | none |
| Unterminated string, bad escape | `TRE0001`, `TRE0015` | a closing `"`, or the supported escapes / end of rule or the escape | none |
| Wrong operand count, `XOR` and the other binary operators | `TRE0006`, `TRE0014` | `2 operands` / `3 operands` | `PARITY` / `ExactlyOne` for `XOR`, parentheses for the others |
| Ambiguous mixing without parentheses | `TRE0007` | parentheses around one of the groups / the operators sharing a level | hint showing the parenthesised text |
| Threshold or `BETWEEN` bounds, non-integer bound | `TRE0008`, `TRE0001` | the valid range, or an integer / the value | none |
| Declared `Collapse` | `TRE0001` (DSL), `TRE0014` (JSON/YAML) | a rule without `Collapse` / `Collapse` | hint to call `Decision.Collapse(policy)` on the result |
| Declared `Project` | `TRE0001` (DSL), `TRE0014` (JSON/YAML) | a rule without `Project` / `Project` | hint to use `COALESCE(x, True)` / `COALESCE(x, False)` or `Decision.Project(unknownAs)` |
| Missing, unknown or mistyped predicate argument | `TRE0003`, `TRE0005`, `TRE0004` | the argument or kind / what was written | nearest declared argument name |
| Variable reference naming an undeclared data source | `TRE0024` | a declared data source name / the name written | nearest declared source name, or a hint to declare it |
| Variable reference whose query fails its source's validator | `TRE0025` | a query valid for the data source / the query written | none |

## Benchmarks

`benchmarks/TruthWeaver.Benchmarks` is a [BenchmarkDotNet](https://benchmarkdotnet.org/)
console project (dev-only — never packed, never referenced by `src/`) measuring:

- **Compile-time cost** (`CompileBenchmarks.Compile`) — `RuleCompiler.CompileJson`'s full
  Parse → Validate → Analyze → Build pipeline, including the BDD-based tautology/contradiction
  analyzer, across a small (10-term) and a large (200-term) representative rule.
- **Eval-time memoized term lookup** (`EvaluationBenchmarks.EvaluateAsync`) — `CompiledRule.EvaluateAsync`
  against a rule whose branches all share one term, at increasing branch fan-out, exercising the
  per-evaluation term memoization ADR-0002 describes.
- **Operator families** (`OperatorBenchmarks`) - evaluation cost of the connective, cardinality, threshold,
  external, `If` and `PARITY` operator families.
- **Rewrites** (`RewriteBenchmarks`) - `ExpandToPrimitives`/`ExpandToNand`/`ExpandToNor`, `CompressToDerived`,
  `Canonicalize` and `Simplify` on one mixed-operator rule.
- **Diagnostics formatting** (`DiagnosticsBenchmarks`) - `CompilationResult.FormatDiagnostics` with and without source text.

A committed baseline (captured with `--job Short`) lives at
[`benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md`](benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md).

Run the full suite (this repo's `net11.0` preview target isn't yet recognized by BenchmarkDotNet's
default toolchain, so `--inProcess` is required — see the code comment on `CompileBenchmarks`/
`EvaluationBenchmarks`' host project for why):

```powershell
dotnet build benchmarks/TruthWeaver.Benchmarks -c Release
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --inProcess
```

Useful variations:

```powershell
# Discover benchmark names without running them
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --list flat

# Fast smoke test (one iteration per case, no meaningful measurement)
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Dry --inProcess

# Regenerate the committed baseline
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Short --inProcess --exporters github --artifacts ./benchmarks/TruthWeaver.Benchmarks/results
```

## Glossary

The [glossary](docs/glossary.md) explains the vocabulary of TruthWeaver, including the terms that are new or easy to confuse. [CONTEXT.md](CONTEXT.md) gives the reasoning behind each engine term.

## Appendix: Truth tables

Kleene three-valued truth tables for every binary/unary operator, in both
logical-name and boolean-algebra notation. `T` = `TruthValue.True`, `F` =
`TruthValue.False`, `?` = `TruthValue.Unknown`. Algebra notation: `∧` = AND,
`∨` = OR, `¬` = NOT, `⊕` = XOR, `↔` = EQUIVALENT (biconditional / IFF / legacy XNOR), `→` = IMPLIES, `↑` = NAND, `↓` = NOR, `1` = true,
`0` = false. Full reasoning: [ADR-0001](docs/adr/0001-kleene-failure-model.md).

### Unary: `NOT`

| a | `NOT a` | ¬a |
| :-: | :-: | :-: |
| T | F | ¬1 = 0 |
| F | T | ¬0 = 1 |
| ? | ? | ¬? = ? |

### Binary: `AND`

| a | b | `a AND b` | a∧b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1∧1 = 1 |
| T | F | F | 1∧0 = 0 |
| T | ? | ? | 1∧? = ? |
| F | T | F | 0∧1 = 0 |
| F | F | F | 0∧0 = 0 |
| F | ? | F | 0∧? = 0 |
| ? | T | ? | ?∧1 = ? |
| ? | F | F | ?∧0 = 0 |
| ? | ? | ? | ?∧? = ? |

### Binary: `OR`

| a | b | `a OR b` | a∨b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1∨1 = 1 |
| T | F | T | 1∨0 = 1 |
| T | ? | T | 1∨? = 1 |
| F | T | T | 0∨1 = 1 |
| F | F | F | 0∨0 = 0 |
| F | ? | ? | 0∨? = ? |
| ? | T | T | ?∨1 = 1 |
| ? | F | ? | ?∨0 = ? |
| ? | ? | ? | ?∨? = ? |

### Binary: `XOR`

| a | b | `a XOR b` | a⊕b |
| :-: | :-: | :-: | :-: |
| T | T | F | 1⊕1 = 0 |
| T | F | T | 1⊕0 = 1 |
| T | ? | ? | 1⊕? = ? |
| F | T | T | 0⊕1 = 1 |
| F | F | F | 0⊕0 = 0 |
| F | ? | ? | 0⊕? = ? |
| ? | T | ? | ?⊕1 = ? |
| ? | F | ? | ?⊕0 = ? |
| ? | ? | ? | ?⊕? = ? |

### Binary: `EQUIVALENT` (`NOT (a XOR b)`)

| a | b | `a EQUIVALENT b` | a↔b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1↔1 = 1 |
| T | F | F | 1↔0 = 0 |
| T | ? | ? | 1↔? = ? |
| F | T | F | 0↔1 = 0 |
| F | F | T | 0↔0 = 1 |
| F | ? | ? | 0↔? = ? |
| ? | T | ? | ?↔1 = ? |
| ? | F | ? | ?↔0 = ? |
| ? | ? | ? | ?↔? = ? |

### Binary: `IMPLIES` (`NOT a OR b`)

| a | b | `a IMPLIES b` | a→b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1→1 = 1 |
| T | F | F | 1→0 = 0 |
| T | ? | ? | 1→? = ? |
| F | T | T | 0→1 = 1 |
| F | F | T | 0→0 = 1 |
| F | ? | T | 0→? = 1 |
| ? | T | T | ?→1 = 1 |
| ? | F | ? | ?→0 = ? |
| ? | ? | ? | ?→? = ? |

### Binary: `NAND` (`NOT (a AND b)`)

| a | b | `a NAND b` | a↑b |
| :-: | :-: | :-: | :-: |
| T | T | F | 1↑1 = 0 |
| T | F | T | 1↑0 = 1 |
| T | ? | ? | 1↑? = ? |
| F | T | T | 0↑1 = 1 |
| F | F | T | 0↑0 = 1 |
| F | ? | T | 0↑? = 1 |
| ? | T | ? | ?↑1 = ? |
| ? | F | T | ?↑0 = 1 |
| ? | ? | ? | ?↑? = ? |

### Binary: `NOR` (`NOT (a OR b)`)

| a | b | `a NOR b` | a↓b |
| :-: | :-: | :-: | :-: |
| T | T | F | 1↓1 = 0 |
| T | F | F | 1↓0 = 0 |
| T | ? | F | 1↓? = 0 |
| F | T | F | 0↓1 = 0 |
| F | F | T | 0↓0 = 1 |
| F | ? | ? | 0↓? = ? |
| ? | T | F | ?↓1 = 0 |
| ? | F | ? | ?↓0 = ? |
| ? | ? | ? | ?↓? = ? |

### N-ary: `PARITY` (parity)

`PARITY` is `Unknown` whenever any operand is `Unknown`; otherwise it is `True`
exactly when an odd number of operands are `True`. Two operands give the
`XOR` table above; three operands:

| a | b | c | `PARITY(a, b, c)` |
| :-: | :-: | :-: | :-: |
| T | T | T | T |
| T | T | F | F |
| T | F | F | T |
| F | F | F | F |
| T | T | ? | ? |
| F | F | ? | ? |
| T | ? | F | ? |

### N-ary: `ANY`, `ALL`, `NONE` (three operands)

Each is a count of `True` operands compared over the interval
`[definitely true, definitely true + Unknown]`: the result is certain only when
every reachable count agrees.

| a | b | c | `ANY` | `ALL` | `NONE` |
| :-: | :-: | :-: | :-: | :-: | :-: |
| T | F | F | T | F | F |
| F | F | F | F | F | T |
| T | T | T | T | T | F |
| T | ? | F | T | F | F |
| F | ? | F | ? | F | ? |
| T | ? | T | T | ? | F |
| ? | ? | ? | ? | ? | ? |

### Binary: `COALESCE` (`a ?? b`)

Only `Unknown` is replaced; a known first operand always wins. The n-ary form
folds this from the right (`COALESCE(a, b, c)` is `COALESCE(a, COALESCE(b, c))`).

| a \ b | T | F | ? |
| :-: | :-: | :-: | :-: |
| **T** | T | T | T |
| **F** | F | F | F |
| **?** | T | F | ? |

### Unary: inspection (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`)

These test the K3 state itself, so the answer is always a definite `True` or
`False` (never `Unknown`) and can be combined freely with the rest of a rule.
`IsUnknown(a) OR IsKnown(a)` is a genuine tautology.

| a | `IsTrue(a)` | `IsFalse(a)` | `IsUnknown(a)` | `IsKnown(a)` |
| :-: | :-: | :-: | :-: | :-: |
| T | T | F | F | T |
| F | F | T | F | T |
| ? | F | F | T | F |

### Projection: `Decision.Project(unknownAs)` / `COALESCE(a, True|False)`

`Project` is a method on the result. It keeps `True` and `False` and replaces only
`Unknown` with the chosen constant, so its result is always definite. It is the same
value as the in-rule `COALESCE(a, v)`.

| a | `Project(unknownAs: true)` / `COALESCE(a, True)` | `Project(unknownAs: false)` / `COALESCE(a, False)` |
| :-: | :-: | :-: |
| T | T | T |
| F | F | F |
| ? | T | F |

### Collapse: `Decision.Collapse(policy)`

`Collapse` is a method on the result, so its "truth table" maps a K3 result to a
`CollapseOutcome` rather than to another `TruthValue`.

| a | `UnknownAsFalse` | `UnknownAsTrue` | `UnknownIsError` |
| :-: | :-: | :-: | :-: |
| T | True | True | True |
| F | False | False | False |
| ? | False | True | RejectedUnresolved |

### Ternary: `If(c, t, f)` (`c ? t : f`)

A definite condition picks its branch. An `Unknown` condition cannot, so the
result is certain only when both branches agree on a definite value (the
`(t AND f)` consensus term in the definition).

| c | t | f | `If(c, t, f)` |
| :-: | :-: | :-: | :-: |
| T | T | F | T |
| T | F | T | F |
| T | ? | F | ? |
| F | T | F | F |
| F | F | T | T |
| F | T | ? | ? |
| ? | T | T | T |
| ? | F | F | F |
| ? | T | F | ? |
| ? | F | T | ? |
| ? | ? | ? | ? |

This is the strongest extension of the classical conditional: `If(c, t, f)` is
definite exactly when every `True`/`False` resolution of the `Unknown` inputs
gives the same answer (27 of 27 triples agree, pinned by a test). The bare
multiplexer `(c AND t) OR (NOT c AND f)` would give `Unknown` for
`If(Unknown, A, A)`; the consensus term keeps it `A`. The classical
consensus-removal rewrite is therefore invalid in K3, and `Simplify` and
`Canonicalize` never apply it (rationale: [ADR-0005](docs/adr/0005-strong-k3-language-surface.md)
decision 13 and the [spec audit](.scratch/k3-conformance/spec-audit.md), C2).

**SQL `CASE` equivalent.** SQL `CASE WHEN c THEN t ELSE f END` sends an
`Unknown` condition to the `ELSE` branch, so it differs from `If` at four of the
27 triples. Write `If(IsTrue(c), t, f)` to get that behavior.

### N-ary: `BETWEEN(1, 2, a, b, c)`

The count of `True` operands must lie in `[1, 2]` for every reachable count
(`[definitely true, definitely true + Unknown]`) for the result to be certain.

| a | b | c | `BETWEEN(1, 2, a, b, c)` |
| :-: | :-: | :-: | :-: |
| T | F | F | T |
| F | F | F | F |
| T | T | T | F |
| T | ? | F | T |
| F | ? | F | ? |
| T | T | ? | ? |
| ? | ? | ? | ? |

`ExactlyOne(...)` and the threshold family don't get their own table here —
they're n-ary counting operators over the *number* of `True` operands, not
fixed two-input truth tables; their exact Kleene semantics (what counts as
"certain" vs. "still possibly reachable" when some operands are `Unknown`)
are covered by the evaluator's behavior described in
[Evaluation](docs/strong-k3/specification/evaluation.md) and tested directly in
`XorExactlyOneThresholdTests`.

## Design documents

- [CONTEXT.md](CONTEXT.md) — vocabulary, conceptual model, and the
  predicate-author contract.
- [ADR-0001: Kleene failure model](docs/adr/0001-kleene-failure-model.md) —
  why evaluation is three-valued internally and fails closed at the boundary.
- [ADR-0002: Evaluation semantics](docs/adr/0002-evaluation-semantics.md) —
  async predicates, per-evaluation memoization, short-circuit, fault
  handling, predicate registration, and the compile-and-swap rule lifecycle.
- [ADR-0003: Rule syntax and serialization](docs/adr/0003-rule-syntax-and-serialization.md) —
  the DSL grammar, the JSON/YAML tree form and the compile pipeline (its operator set
  is superseded by ADR-0005).
- [ADR-0004: Package boundaries and extensibility](docs/adr/0004-package-boundaries-and-extensibility.md) —
  why the library ships as six packages and how predicates and operators
  are extended.
- [ADR-0005: Strong K3 language surface](docs/adr/0005-strong-k3-language-surface.md) —
  the full K3 operator set, notations, boundaries (`Decision.Project` and `Decision.Collapse` on the result), rewrites,
  structured diagnostics and `TruthValue`-returning predicates.
- [Strong Kleene (K3) reference](docs/strong-k3/README.md) —
  one document per operation with truth tables, formulas, canonical forms and the shared specification.

## License

Apache License 2.0 — see [LICENSE](LICENSE).

See [CLAUDE.md](CLAUDE.md) for development rules, required validation
commands, and formatting/testing conventions.
