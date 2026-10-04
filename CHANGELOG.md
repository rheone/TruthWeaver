# Changelog

All notable changes to TruthWeaver are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Versioning and release status

> [!IMPORTANT]
> **TruthWeaver has not been published.** No package has been pushed to NuGet, the repository has no release tags, and
> every package builds as `1.0.0-dev` (set once in [`Directory.Build.props`](Directory.Build.props)). Nothing below
> has been shipped to a consumer yet, so the "breaking changes" are measured against two reference points rather than
> against a released version:
>
> - **The first engine commit, [`df8f4b9`](https://github.com/rheone/TruthWeaver/commit/df8f4b9)**
>   (`BooleanRulesEngine` v1). Anyone who cloned the project early starts here. See
>   [Migrating from the first engine commit](#migrating-from-the-first-engine-commit).
> - **The commit before the Strong K3 work, `57cf2c9`.** The K3 effort changed most of the public surface. See
>   [Migrating across the Strong K3 work](#migrating-across-the-strong-k3-work).

**Version policy.** The version stays `1.0.0-dev` until the first release. Until `1.0.0` ships, any change may break
compatibility, and this file is where each break is recorded with its migration step. From `1.0.0` the packages follow
[Semantic Versioning](https://semver.org/): breaking changes only in a major version, new backward-compatible features
in a minor version, fixes in a patch version. All five packages share one version.

**Package metadata** (checked on the 1.0.0-dev build): the five packages (`TruthWeaver`, `TruthWeaver.Abstractions`,
`TruthWeaver.Predicates`, `TruthWeaver.Testing`, `TruthWeaver.Yaml`) are packable, target `net11.0`, carry the Apache-2.0
license expression, the repository URL, symbol packages (`.snupkg`), SourceLink and the repository `README.md` as the
package readme. Open items, deliberately not changed here: there is no `PackageIcon` (no icon asset exists), and the
copyright line still reads 2024-2025.

**Consumer requirement.** The packages are built with `EnablePreviewFeatures`, so a consuming project must also set
`<EnablePreviewFeatures>true</EnablePreviewFeatures>`, otherwise every use of the public API fails with `CA2252`
("requires opting into preview features"). This is checked by compiling the README quick-start in a fresh project.

## [Unreleased]

### Added

- Strong Kleene (K3) language surface (see [ADR-0005](docs/adr/0005-strong-k3-language-surface.md)): `Unknown` as a first
  class value and constant; the operators `IMPLIES`, `EQUIVALENT` (`IFF`), `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`,
  `BETWEEN`, `COALESCE`, `If` (`? :`) and the inspections `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`; symbol and Unicode
  input aliases; `[]` and `{}` grouping; depth-cycling delimiters.
- Rewrites on `CompiledRule`: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`
  and `Simplify`.
- `Decision.Collapse(CollapsePolicy)` and `Decision.Project(...)` to resolve `Unknown` at the call site.
- Dual-rail K3 analyzer, structured diagnostics with suggestions and spans (JSON diagnostics carry paths and source
  spans), `DiagnosticFormatter`.
- `RuleEquivalence.Compare` (equivalent, not equivalent with a counter-example, or undecided) and
  `RuleDiffResult.PreservesMeaning`.
- Opt-in lint rules through `CompilerOptions.Lints` (`TRE0017` to `TRE0023`).
- `CompilerOptions.MaxRewriteNodeCount` (default 100,000) and diagnostic `TRE0016` for oversized expansions.
- `NullBehavior` option on the built-in string, regex and collection predicates; `IEnumerable<RuleBuilder>` overloads
  for the counted operators.
- Benchmarks for the new operators, rewrites and diagnostics formatting.

### Changed

- Every break below. Each one has a migration step in the sections that follow.

### Breaking changes: naming cleanup (ADR-0007)

Public names and diagnostic codes were aligned with the glossary in [ADR-0007](docs/adr/0007-naming-cleanup-and-tre-diagnostic-prefix.md). Behavior is unchanged. No `[Obsolete]` forwarders exist; this section is the migration guide.

#### Diagnostic code prefix `BRE` is now `TRE`

"TRE" stands for "Trinary Rule Expression". Every code keeps its number, so the mapping is a prefix swap. Update any filter, suppression list or string comparison that uses the old prefix.

| Old | New | `DiagnosticCodes` member |
| --- | --- | --- |
| `BRE0001` | `TRE0001` | `SyntaxError` |
| `BRE0002` | `TRE0002` | `UnknownPredicate` |
| `BRE0003` | `TRE0003` | `MissingArgument` |
| `BRE0004` | `TRE0004` | `ArgumentTypeMismatch` |
| `BRE0005` | `TRE0005` | `UnknownArgument` |
| `BRE0006` | `TRE0006` | `InfixArityViolation` |
| `BRE0007` | `TRE0007` | `AmbiguousOperatorMixing` |
| `BRE0008` | `TRE0008` | `InvalidThresholdValue` |
| `BRE0009` | `TRE0009` | `MaxDepthExceeded` |
| `BRE0010` | `TRE0010` | `MaxNodeCountExceeded` |
| `BRE0011` | `TRE0011` | `AnalysisSkippedTooManyTerms` |
| `BRE0012` | `TRE0012` | `StructuralTautology` |
| `BRE0013` | `TRE0013` | `StructuralContradiction` |
| `BRE0014` | `TRE0014` | `MalformedTree` |
| `BRE0015` | `TRE0015` | `InvalidEscapeSequence` |
| `BRE0016` | `TRE0016` | `RewriteTooLarge` |
| `BRE0017` | `TRE0017` | `RedundantInspection` |
| `BRE0018` | `TRE0018` | `RedundantCoalesce` |
| `BRE0019` | `TRE0019` | `ConstantIfCondition` |
| `BRE0020` | `TRE0020` | `IdenticalIfBranches` |
| `BRE0021` | `TRE0021` | `VacuousCardinality` |
| `BRE0022` | `TRE0022` | `DuplicateOperands` |
| `BRE0023` | `TRE0023` | `DoubleNegation` |

Entries elsewhere in this file that name a code were updated to the `TRE` prefix.

#### Type and member renames

| Old | New |
| --- | --- |
| `EvaluatedNode` | `TraceNode` |
| `Decision.EvaluatedTree` | `Decision.TraceTree` |
| `NodeDescription` (on `TraceEntry` and the tree node) | `Text` |
| `RuleDescription` | `OutlineNode` (the root node `CompiledRule.Outline()` returns is the rule outline) |
| `CompiledRule.Describe()` | `CompiledRule.Outline()` |
| `EvaluationMode.Default` | `EvaluationMode.ShortCircuit` |
| `ResolvedValuePredicates` (`resolve`, `TResolved`) | `SelectedValuePredicates` (`select`, `TSelected`) |
| `UniversalGateExpander` (internal; the public `ExpandToNand` and `ExpandToNor` are unchanged) | `NandNorExpander` |

### Breaking changes at a glance

| Area | Change | Section |
| --- | --- | --- |
| Predicates | `IPredicate<T>.EvaluateAsync` returns `ValueTask<TruthValue>`, not `ValueTask<bool>` | [K3 1](#1-predicates-return-truthvalue) |
| Constants | `ConstantExpression` holds a `TruthValue`; canonical text is `True`/`False`/`Unknown` | [K3 2](#2-constants-are-truthvalue-and-print-capitalised) |
| Biconditional | `XnorExpression` is `EquivalentExpression`; canonical label and JSON/YAML op are `EQUIVALENT`/`equivalent` | [K3 3](#3-xnor-became-equivalent) |
| Diagnostics | `DiagnosticCodes.XorArityViolation` is `InfixArityViolation` (code `TRE0006` unchanged) | [K3 4](#4-the-arity-diagnostic-constant-was-renamed) |
| Analyzer | Tautology and contradiction diagnostics are now Strong K3 results | [K3 5](#5-tautology-and-contradiction-diagnostics-are-k3-results) |
| Predicate names | New operator words are reserved in rule text | [K3 6](#6-new-operator-words-are-reserved) |
| Built-in predicates | `EqualsConfigurable` has no `culture` argument and is ordinal | [K3 7](#7-equalsconfigurable-lost-its-culture-argument) |
| Public records | `CompilerOptions` and `RuleDiffResult` gained positional parameters | [K3 8](#8-records-gained-positional-parameters) |
| Testing | `FakePredicates` answer `Unknown` without a fault | [K3 9](#9-fakepredicates-answer-unknown-without-a-fault) |
| JSON/YAML | New op names; malformed `k` is a diagnostic | [K3 10](#10-jsonyaml-tree-format) |
| Interim names | Names that existed only in unreleased K3 commits | [Interim](#names-that-existed-only-in-unreleased-k3-commits) |

## Migrating across the Strong K3 work

Reference point: commit `57cf2c9`, the last commit before the K3 work began. Each entry gives the old form, the new
form and the migration step.

### 1. Predicates return `TruthValue`

- **Old:** `ValueTask<bool> EvaluateAsync(TContext, PredicateArguments, CancellationToken)`; predicate factories returned
  `Func<..., ValueTask<bool>>`; an indeterminate answer had to be signalled by throwing.
- **New:** `ValueTask<TruthValue> EvaluateAsync(...)`; factories return `Func<..., ValueTask<TruthValue>>`. Return
  `TruthValue.Unknown` for a legitimately indeterminate answer (no `Fault` is recorded); throw only for a real failure
  (recorded as a `Fault`, the term is `Unknown`).
- **Migrate:** change the return type and map the result, for example
  `ValueTask.FromResult(ok ? TruthValue.True : TruthValue.False)`. Registered delegates change the same way. Predicates
  that threw to mean "cannot tell" can now return `Unknown` instead.

### 2. Constants are `TruthValue` and print capitalised

- **Old:** `ConstantExpression(bool Value)`; canonical text `true` / `false`.
- **New:** `ConstantExpression(TruthValue Value)`; canonical text `True` / `False` / `Unknown`. `RuleBuilder.Constant(bool)`
  still exists, and `Constant(TruthValue)` was added.
- **Migrate:** pass `TruthValue` where you build or match `ConstantExpression`. Rule text is unaffected on input (the
  words are case-insensitive), and JSON/YAML keep `true`/`false` booleans; `Unknown` is written `"unknown"`. Anything that
  stored or compared `CanonicalText` (cache keys, golden files, de-duplication) must be regenerated, because the text
  changed for every rule containing a constant.

### 3. `XNOR` became `EQUIVALENT`

- **Old:** `XnorExpression`; canonical text `a XNOR b`; JSON/YAML op `xnor`; `RuleBuilder.Xnor`.
- **New:** `EquivalentExpression`; canonical text `a EQUIVALENT b`; JSON/YAML op `equivalent`; `RuleBuilder.Equivalent`.
  `XNOR` and `IFF` (and `↔`) remain accepted on input in rule text, JSON and YAML, and `RuleBuilder.Xnor` forwards to
  `Equivalent`, so existing rules keep compiling.
- **Migrate:** rename `XnorExpression` to `EquivalentExpression` in code. Regenerate any stored canonical text or printed
  JSON/YAML (see entry 2); the printers now write only the new spelling.

### 4. The arity diagnostic constant was renamed

- **Old:** `DiagnosticCodes.XorArityViolation`.
- **New:** `DiagnosticCodes.InfixArityViolation`. The code string `TRE0006` is unchanged. It now also covers `EQUIVALENT`,
  `IMPLIES`, `NAND` and `NOR` given other than two operands.
- **Migrate:** rename the constant. Comparisons against the string `"TRE0006"` need no change.

### 5. Tautology and contradiction diagnostics are K3 results

- **Old:** the analyzer treated rules as two-valued, so `a OR NOT a` was reported as a tautology and `a AND NOT a` as a
  contradiction.
- **New:** the analyzer is dual-rail Strong K3. A rule is reported only when it is `True` (or `False`) for every
  `True`/`False`/`Unknown` assignment, so `a OR NOT a` is no longer flagged: it is `Unknown` when `a` is. The diagnostic
  codes and constant names are unchanged.
- **Migrate:** none in code. Expect fewer warnings; do not rely on the old two-valued findings.

### 6. New operator words are reserved

- **Old:** only `AND`, `OR`, `NOT`, `XOR`, `XNOR`, `ExactlyOne` and the threshold names were reserved.
- **New:** `IMPLIES`, `EQUIVALENT`, `IFF`, `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `If`,
  `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown` and the constants `True`/`False`/`Unknown` are keywords, matched
  case-insensitively. A predicate registered under one of these names can no longer be used by that bare name in rule
  text (a registered predicate named `any` now fails to parse `any`).
- **Migrate:** rename the colliding predicate; persisted rules that used the old name must be rewritten.

### 7. `EqualsConfigurable` lost its `culture` argument

- **Old:** `EqualsConfigurable(value, ignoreCase, culture, trim)` honoured a `culture` string.
- **New:** comparison is ordinal; the arguments are `value`, `ignoreCase` (default `true`) and `trim` (default `false`).
  A rule that passes `culture` is rejected with an unknown-argument diagnostic that advises removing it. (Between the
  two it briefly required `culture` to be empty; that interim form never shipped.)
- **Migrate:** delete `culture:` from the rule. If you relied on culture-sensitive matching (for example the Turkish
  dotless I), compare in your own predicate instead.

### 8. Records gained positional parameters

- **Old:** `CompilerOptions(MaxDepth, MaxNodeCount, MaxAnalysisTerms, Mode)` and `RuleDiffResult(entries)`.
- **New:** `CompilerOptions` adds `MaxRewriteNodeCount = 100_000` and `Lints = LintRules.None`; `RuleDiffResult` adds
  `bool? PreservesMeaning`.
- **Migrate:** source code that uses named or `with` construction is unaffected. Positional construction and
  deconstruction need the new parameters, and assemblies compiled against the old shape must be rebuilt.

### 9. `FakePredicates` answer `Unknown` without a fault

- **Old:** `FakePredicates.Returning(TruthValue.Unknown)` and a `Scripted` `Unknown` entry threw
  `SimulatedPredicateFaultException`, which recorded a `Fault`.
- **New:** they return `Unknown` directly, like a real predicate, and record no `Fault`. Use `FakePredicates.Faulting`
  to simulate a failure.
- **Migrate:** tests that asserted a fault for an `Unknown` fake should switch to `Faulting`.

### 10. JSON/YAML tree format

- **New op names** (all accepted case-insensitively on input): `equivalent` (`iff`, `xnor`), `implies`, `nand`, `nor`,
  `parity`, `any`, `all`, `none`, `between`, `coalesce`, `if`, `isTrue`, `isFalse`, `isUnknown`, `isKnown`. The printers
  write `equivalent` rather than `xnor`. The constant leaf also accepts the strings `"true"`, `"false"` and `"unknown"`.
  `rule-tree.schema.json` was updated; re-validate documents that embed an older copy of the schema.
- **Malformed `k`:** a non-numeric, fractional or out-of-range `k` on a threshold node is now reported as a diagnostic by
  the JSON and YAML parsers instead of throwing from the parse (a fractional or oversized number used to surface as an
  exception). Code that caught that exception should read `CompilationResult.Diagnostics`.
- **Also new:** tree diagnostics carry a JSON Path style `Path`, and JSON diagnostics also carry a source span.
- **Migrate:** persisted `xnor` documents keep working. Regenerate any golden files that were produced by the printers.

### Names that existed only in unreleased K3 commits

These were added and then replaced during the K3 effort, before any release. They are listed because a clone taken
mid-effort may use them; a consumer starting from `57cf2c9` or `df8f4b9` never saw them.

| Interim form | Final form | Why |
| --- | --- | --- |
| `NXOR` operator (n-ary parity) | `PARITY`; the old spelling is rejected with a diagnostic that names `PARITY` | `NXOR` conventionally means negated XOR |
| `Collapse(expr, policy)` rule operator; `Decision.Outcome`; `CompiledRule.CollapsePolicy` | `Decision.Collapse(CollapsePolicy)` on the result; the rule language no longer has it and rejects it | A rule cannot carry an evaluation boundary |
| `Project(expr, True\|False)` rule operator | `Decision.Project(...)` on the result; rejected in rule text | Same reason |
| `CompiledRule.PrintText(GroupingStyle)` | `CompiledRule.PrintRuleText(GroupingStyle)` | Naming pass |
| `ExpandToPrimitives()`, `ExpandToNand()`, `ExpandToNor()` returning `CompiledRule<TContext>` | Each takes an optional `CompilerOptions` and returns `CompilationResult<TContext>`; read `.CompiledRule`. An over-cap result is a `TRE0016` error with no rule | Size guard (`MaxRewriteNodeCount`) |
| JSON/YAML keys `collapse` and `project` | Rejected with a diagnostic | As above |
| `If` printed with its word label in C-style trees | `If` prints as `?:` in C-style trees | Notation pass |

## Migrating from the first engine commit

Reference point: `df8f4b9`. In addition to everything under [Migrating across the Strong K3 work](#migrating-across-the-strong-k3-work),
these breaks landed between that commit and `57cf2c9`:

- **Rename `BooleanRulesEngine` to `TruthWeaver`** (commit `4cfc1b9`). Solution, project, package and namespace names
  changed (`BooleanRulesEngine.*` to `TruthWeaver.*`), including the Predicates and Testing packages and the
  `BooleanRulesEngine.Ast` namespace. **Migrate:** update `PackageReference`/`ProjectReference` entries and `using`
  directives.
- **Predicate `Description` and `Label` are required** (commits `4176ea2`, `dc02e7b`). `PredicateSchema` and
  `PredicateArgumentSchema` require a `Description`, and `PredicateSchema` requires a `Label`. **Migrate:** supply both
  when building schemas.
- **Canonical text parenthesises mixed `AND`/`OR`** (commit `578cce8`). An `AND` operand under an `OR` (and the reverse)
  is parenthesised even where precedence would make it unambiguous. **Migrate:** regenerate stored canonical text.
- **String literal escaping** (commit `f3272f1`). `LiteralValue.ToString()` and the canonical printer escape `\` and
  `"` in string arguments, and an unknown escape such as `\q` in rule text is now diagnostic `TRE0015` instead of the
  backslash being dropped silently. **Migrate:** fix rule text that relied on an unrecognised escape.

Other changes in that range were additive (threshold family, `XNOR`/`IFF`, `RuleBuilder`, Guid literals,
`Describe()`, rule diff, JSON Schema, metrics, `OperatorStyle`, and the new Predicates, Testing and Yaml packages).

## How this list was verified

Each entry was checked against `git log` and `git diff 57cf2c9 HEAD` and, where it describes behaviour, by running it
(for example a registered predicate named `any` no longer parses, the JSON printer writes `equivalent`, and canonical
text prints `EQUIVALENT`). The interim-name table was checked against the k3-followups tickets 04, 05, 06, 08, 20, 21
and k3-hardening ticket 03. The README quick-start was compiled in a fresh project against the current API.
