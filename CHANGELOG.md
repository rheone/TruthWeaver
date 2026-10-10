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
in a minor version, fixes in a patch version. All six packages share one version.

**Package metadata** (checked on the 1.0.0-dev build): the six packages (`TruthWeaver`, `TruthWeaver.Abstractions`,
`TruthWeaver.DataSources.Json`, `TruthWeaver.Predicates`, `TruthWeaver.Testing`, `TruthWeaver.Yaml`) are packable, target `net11.0`, carry the Apache-2.0
license expression, the repository URL, symbol packages (`.snupkg`), SourceLink and the repository `README.md` as the
package readme. Open items, deliberately not changed here: there is no `PackageIcon` (no icon asset exists), and the
copyright line reads 2026.

**Consumer requirement.** None. The packages no longer set `EnablePreviewFeatures`, so they carry no requires-preview-features marker and a consuming project needs no preview opt-in (no `CA2252`). This is checked by compiling the README quick-start in a fresh project.

## [Unreleased]

### Added

- Reversed literal bounds on `Between` and `Outside` (`NumericPredicates` for `Int64` and `Decimal`, and
  `DateTimePredicates`) are now a compile-time error, the new `DiagnosticCodes.InvalidArgumentValue` (`TRE0026`), at the
  predicate call in rule text, JSON, YAML and `RuleBuilder` rules, with a suggestion to swap the bounds. `Compile` returns
  no rule. Equal bounds still compile. A bound that is not a literal, such as a value read from a data source, still
  throws `ArgumentException` at evaluation (`Unknown` plus a `Fault`). The bounds are never swapped.
- `PredicateSchema.ArgumentValidator` and `PredicateArgumentProblem`: an optional check over the literal argument values
  of a predicate call. Each problem it returns is a `TRE0026` error. It is `null` by default, so existing schemas are
  unchanged.

- `NotEqualsIgnoreCase`, `NotStartsWith`, `NotEndsWith` and `NotEqualsConfigurable` in `StringPredicates` and `NotSetEquals`
  in `CollectionPredicates`: the Strong Kleene complements of their positive members.

- `Arg.TryFrom` (returns the validator's `QueryProblem` list instead of throwing `ArgumentException`) and
  `IDataSource.TryGetAsync<T>` in `TruthWeaver.Building`, which returns a `DataReadResult<T>` (`Succeeded`, `Value`,
  `FailureKind`, `ErrorMessage`) instead of throwing `InvalidOperationException`. `Arg.From` and `GetAsync<T>` still throw.
- `TypePredicates`: the type tests `IsGuid`, `IsNumeric`, `IsUrl`, `IsString` and `IsDateTimeOffset` and their `IsNot...` twins
  (the Strong Kleene complement). Each has a `string?` (parse-based) and an `object?` (runtime-type) overload; a null
  selected value is `Unknown`.
- `Try` forms for reading optional or mixed-kind arguments without exception handling: `LiteralValue.TryAsString`, `TryAsInt64`,
  `TryAsDecimal`, `TryAsBoolean`, `TryAsDateTimeOffset`, `TryAsGuid` and `TryAsArray`, and `PredicateArguments.TryGetString`,
  `TryGetInt64`, `TryGetDecimal`, `TryGetBool`, `TryGetDateTimeOffset`, `TryGetGuid`, the matching `TryGet…Array` forms and
  `TryGetRaw`. Each returns `false` for a missing name or a different kind. The existing `As…` and `Get…` members still throw.
- Collection and date/time predicates in `TruthWeaver.Predicates`: `CollectionPredicates` adds `IsEmpty`, `Contains`, `ContainsAny`, `ContainsAll`, `IsSubsetOf`, scalar `In` and the `Count*` comparisons, and the new `DateTimePredicates` adds `After`, `Before` and `Between` over `DateTimeOffset`. Each has a `NotX` Strong Kleene twin (`Outside` for `Between`). Null selections return `Unknown` by default, with `NullBehavior.False` as the host option, and reversed `Between` bounds throw `ArgumentException`. `DateTimePredicates` also adds the clock predicates `AfterNow` and `BeforeNow` with twins `NotAfterNow` and `NotBeforeNow`; each factory requires a `TimeProvider` and there is no ambient clock.
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
- Opt-in lint rules through `CompilerOptions.Lints` (`TRE0017` to `TRE0023`, `TRE0028` to `TRE0030`), `Diagnostic.EnclosedBy`, and the `CompilerOptions` values `DeepNestingFraction` and `WideChainOperandLimit`.
- Rewrite tooling: `CompiledRule.ToNnf`, `ToCnf` and `ToDnf` with `NormalFormOptions` (diagnostic `TRE0031` when a
  threshold stays an atom), `CompiledRule.SimplifyWithSteps` (`RewriteStep`, `RewriteLaw`), and
  `RewriteAssertions.AssertSound` with `RewriteExpectations` in `TruthWeaver.Testing`.
- `CompilerOptions.MaxRewriteNodeCount` (default 100,000) and diagnostic `TRE0016` for oversized expansions.
- Predicate catalog additions: the string members `IsEmpty`, `IsNotEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`,
  `IsNotNullOrWhiteSpace`, `NotEqual`, `NotContains` and `RegexPredicates.NotMatches`; `NumericPredicates` (`Int64` and
  `Decimal` equality, ordering, `Between`/`Outside`, `In`/`NotIn`, null and default tests); and `ScalarPredicates`
  (`Boolean`, `Guid` and `DateTimeOffset` equality, `In`/`NotIn`, null and default tests). Every positive member has a
  `NotX` twin that is the Strong Kleene complement.
- `NullBehavior` option on the built-in string, regex and collection predicates; `IEnumerable<RuleBuilder>` overloads
  for the counted operators.
- Benchmarks for the new operators, rewrites and diagnostics formatting.
- `RuleDiff.Compare` takes an optional `CompilerOptions`, passed to the equivalence check, so a raised `MaxAnalysisTerms` decides `PreservesMeaning` for large rules.
- Data sources for expression variables (see [ADR-0006](docs/adr/0006-data-sources-for-expression-variables.md) and the
  [guide](docs/data-sources.md)), DSL part: a term argument can be `from("source", "query")`, resolved on every evaluation from
  a named `IDataSource` passed to the new `CompiledRule.EvaluateAsync(context, services, dataSources, options, cancellationToken)`
  overload. New in `TruthWeaver.Abstractions`: `IDataSource`, `DataQueryResult`, `DataQueryErrorKind`, `DataSources`,
  `VariableReference`, `VariableFailureKind` and `VariableResolutionException` (the `Fault.Exception` for a failed lookup), and
  `TermIdentity.Variables` (references are part of a term's identity by source name and query text). Source names are declared
  with `CompilerOptions.DataSources` (`DataSourceDeclarations`); an undeclared name is `TRE0024` with a "did you mean". A scalar
  argument needs exactly one match, an array argument collects all matches (none gives an empty array), conversions are those of
  DSL literals and no wider, and each `(source, query)` pair is queried once per evaluation (a failure is replayed, not retried). A source that errors,
  throws or times out by itself gives `Unknown` plus a `Fault`; cancelling the evaluation cancels it, as for predicates. `TruthWeaver.Testing` adds
  `FakeDataSource`. JSON/YAML input of variables, query validators and `RuleBuilder` support follow.
- Executable data-source documentation (data-sources 09): the rule, JSON and YAML examples in `docs/data-sources.md` and the README are checked by `DocExampleTests` (the checker now declares the `user` and `request` sources with `JsonQueryValidator`, and knows `ageAtLeast` and `hasRole`), and the guide's C# snippets run in `DataSourcesGuideTests`. The new `TruthWeaver.DataSources.Json` package is listed with the others.
- `RuleBuilder` support for data sources (data-sources 08): `Arg.From(source, query, validator = null)` (in `TruthWeaver.Building`) returns a `VariableReference` that
  `RuleBuilder.Predicate` renders as the `{ "from", "query" }` argument form, so it compiles to the same canonical text as the DSL (an optional
  `IQueryValidator` rejects a malformed query with `ArgumentException` immediately). `IDataSource.GetAsync<T>(query, cancellationToken)` reads a value at
  build time for use as an ordinary literal. `RuleBuilder` argument values now also accept `Guid`.
- Trace redaction (data-sources 07): `EvaluationOptions.IncludeResolvedValues` (default `false`) adds the resolved value after each variable
  reference in the trace text (`min: from("user", "$.minAge") = 18`). By default the trace names only the reference, and fault messages never contain values.
- `YamlDataSource` in `TruthWeaver.Yaml` (data-sources 06): `Parse(yaml)` and `Create(YamlNode)` read a YAML document into the JSON data
  model (quoted scalars are strings, plain scalars follow the core schema limited to JSON's types, an alias is a copy of its
  anchor) and answer queries with the JSON package's JSONPath engine, so one query gives the same result against equivalent JSON
  and YAML and `JsonQueryValidator` serves YAML sources too. `TruthWeaver.Yaml` now references `TruthWeaver.DataSources.Json`.
- Compile-time query validation (data-sources 05): `IQueryValidator` and `QueryProblem` in `TruthWeaver.Abstractions`; a source name
  can be declared with a validator (`DataSourceDeclarations.Add(name, validator)` or `declarations[name] = validator`), and a
  malformed query is then a `TRE0025` error (`DiagnosticCodes.MalformedDataQuery`) at the query string, in the DSL, JSON and YAML.
  `TruthWeaver.DataSources.Json` ships `JsonQueryValidator.Instance`, which parses RFC 9535 JSONPath without a document.
- New package `TruthWeaver.DataSources.Json` (data-sources 04): `JsonDataSource` (`Parse(json)`, `Create(JsonNode?)`) answers JSONPath
  (RFC 9535) queries over a JSON document and converts each matched node to a `LiteralValue` (string, `long`, `decimal`, boolean;
  an object, array or `null` is `DataQueryErrorKind.UnsupportedType`). `ScopeAsync` roots a new source at the single node a query
  matches. It depends on `TruthWeaver.Abstractions` and Meziantou.Framework.JsonPath 3.0.7 (MIT, no further dependencies; it replaced
  JsonPath.Net, whose 3.x binaries ship under the Open Source Maintenance Fee EULA), and the core `TruthWeaver` package gains no third-party dependency.
- Variable references in JSON and YAML (data-sources 03): an argument value of `{ "from": "user", "query": "$.minAge" }` (the same
  mapping in YAML) is a variable reference and compiles to the same tree as `from("user", "$.minAge")`; `rule-tree.schema.json`
  accepts it (a reference is not allowed inside an array literal). A malformed reference is a `TRE0014` diagnostic at the wrong
  member, and an undeclared source name (`TRE0024`) points at the `from` member.

- `JsonDataSource.TryParse(text, out source, out error)` and `YamlDataSource.TryParse(text, out source, out error)` for host code that
  reads untrusted or user-edited documents. They return `false` for malformed JSON or YAML, a duplicate YAML key and a
  self-referential alias. The error text gives the position and never repeats document content. `Parse` still throws.
- `DataScopeResult` (in `TruthWeaver.Abstractions`) and the `DataQueryErrorKind` values `NoMatch` and `AmbiguousMatch`; see the
  breaking change to `IDataSource.ScopeAsync` below. `FakeDataSource.FailingScope(query, message, kind)` scripts a failed scope.

### Changed

- `RewriteAssertions.AssertSound` gains an overload for a rewrite that returns a `CompilationResult<TContext>`, so
  `r => r.ToCnf()` and the other capped rewrites need no `.CompiledRule!`. A result with no rule or an error
  diagnostic fails the assertion with a `'compiles'` check that shows the diagnostics. A call that passes a bare `null`
  as the rewrite is now ambiguous: cast it to the delegate type. `docs/diagnostics.md` holds one table of the compile
  and evaluation limits, and `docs/rewriting-rules.md` states what each rewrite returns.
- The XML documentation of the expanding rewrites and the normal forms, and `docs/rewriting-rules.md`, state that a
  rewrite result over `CompilerOptions.MaxNodeCount` (512 by default) compiles back from its text only when
  `MaxNodeCount` is raised. The two caps stay independent. No behavior changed.
- Breaking: `NullBehavior.Unknown` is the zero value (`0`) and `NullBehavior.False` is `1`. They were `1` and `0`, so
  `default(NullBehavior)` was `False` while the documented default is `Unknown`. A forgotten or defaulted value now means
  `Unknown`. Migration: nothing changes when you name the members. Replace any stored or cast numeric value (a
  `(NullBehavior)0` that meant `False` is now `Unknown`) with the member name, and re-read a persisted `NullBehavior` as
  its name rather than its number.
- Breaking: the collection `IsEmpty` and `IsNotEmpty` take a `NullBehavior` that defaults to `Unknown`, as the string
  `IsEmpty` and the count predicates do. A null collection answered `True` for `IsEmpty` and `False` for `IsNotEmpty`
  before; it is now `Unknown` for both, with no fault. A missing value is `Unknown` unless the predicate is a null test.
  Migration: register the predicates with `nullBehavior: NullBehavior.False` to read a null collection as "not empty"
  (`IsEmpty` `False`, `IsNotEmpty` `True`). To treat a null collection as empty, have the selector return an empty
  collection (`c => c.Tags ?? []`).
- Breaking: `EvaluationOptions.FaultBudget = N` aborts evaluation when the Nth fault is recorded, as ADR-0002 and the
  XML docs state. It aborted on the (N+1)th before, so `FaultBudget: 1` tolerated one fault. A budget below 1 now makes
  `CompiledRule.EvaluateAsync` throw `ArgumentOutOfRangeException`; `0` used to abort on the first fault. `null` is still
  unlimited. The `Timeout` docs now say that an expired timeout throws `OperationCanceledException` and is not a fault.
  Migration: add 1 to a budget you set to tolerate N faults (`FaultBudget: 1` becomes `2`), and replace `0` with `1`.
- Breaking: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `ToNnf`, `ToCnf` and `ToDnf` cap their result at the
  rule's own `CompilerOptions.MaxRewriteNodeCount`, so a rule compiled with a raised cap expands past 100,000 nodes with
  no argument. Their `CompilerOptions? options` parameter is now `int? maxNodeCount`, the cap for that one call.
  `RuleEquivalence.Compare`, `RuleDiff.Compare`, `RuleAssertions.AssertEquivalent` and `RewriteAssertions.AssertSound`
  take `int? maxAnalysisTerms` instead of `CompilerOptions? options`; they read nothing else. Migration: replace
  `rule.ExpandToNand(new CompilerOptions(MaxRewriteNodeCount: n))` with `rule.ExpandToNand(n)` or compile the rule with
  that option, and replace `new CompilerOptions(MaxAnalysisTerms: n)` with `n`.
- Breaking: the `RuleBuilder` array (`params`) and sequence (`IEnumerable<RuleBuilder>`) overloads of an operator give
  the same rule. `And`, `Or`, `Any`, `All` and `None` fold in both: an empty list is the identity constant (`None` of
  one operand is its negation, the others are the operand itself). `Parity`, `ExactlyOne` and `Coalesce` no longer fold
  a short sequence: they build the node and the compiler reports `MalformedTree`, as the array form does. `GreaterThan`
  and `LessThan` gain the `IEnumerable<RuleBuilder>` overload. An argument value of an unsupported type, including
  `null`, is an `ArgumentTypeMismatch` diagnostic from `Compile` and no longer an `ArgumentException`. Migration: check
  the count before you build a `Parity`, `ExactlyOne` or `Coalesce` from a list that can have fewer than two items, and
  expect a one-operand array under `And`, `Or`, `Any`, `All` or `None` to fold to the operand.
- Breaking: a JSON or YAML rule node that has a key its kind does not define is a `MalformedTree` (`TRE0014`) compile
  error at that key, as `rule-tree.schema.json` already states. This covers an unknown key, a misspelt `args`, a
  `predicate` next to an `op`, and a stray `k`, `min` or `max` on an operator that takes none. The key was ignored
  before. Migration: delete the key, or correct its spelling (the diagnostic suggests the nearest valid key).
- Breaking: a predicate argument that is named twice is the new error `DuplicateArgument` (`TRE0032`), in rule text,
  JSON, YAML and `RuleBuilder`. The last value won before. `RuleBuilder.ToJson()` throws `InvalidOperationException`
  for a duplicate, because JSON cannot hold both; `Compile` returns the diagnostic. Migration: keep one occurrence of
  each argument.
- Breaking: a date-time literal must end in `Z` or carry an offset such as `+02:00`. Text with no offset, such as
  `"2026-01-01"` or `"2026-01-01T09:00"`, is an `ArgumentTypeMismatch` compile error that names the fix, in the DSL,
  JSON, YAML and `RuleBuilder`. The same rule applies to a string a data source resolves for a date-time argument
  (the variable faults to `Unknown`). It was read in the host time zone, so one stored rule could mean different
  instants on different hosts. Migration: append `Z` (UTC) or the offset you meant to each date-time literal and each
  stored date-time value.
- Breaking: in the rule text, a threshold `k` that is not a whole number in the `int` range, such as `AtLeast(1.5, a, b)`
  or `AtLeast(99999999999, a, b)`, is an `InvalidThresholdValue` (`TRE0008`), the same as a bad `BETWEEN` bound. It used to
  compile as `k = 0`. `k`, `min` and `max` share one integer parser that uses the invariant culture. Migration: write `k` as an
  integer literal.
- Breaking: one authoring mistake has one diagnostic code in rule text, JSON, YAML and `RuleBuilder`. A wrong operand
  count for any operator is `InfixArityViolation` (`TRE0006`); it was `MalformedTree` (`TRE0014`) for every operator
  except the binary ones. A threshold `k` or `BETWEEN` bound that is missing or not a whole number is
  `InvalidThresholdValue` (`TRE0008`); it was `SyntaxError` (`TRE0001`) in rule text and `MalformedTree` in JSON and
  YAML. A JSON or YAML `op` that does not exist, and a declared `Collapse`, `Project` or `NXOR` in any format, is
  `UnknownPredicate` (`TRE0002`); they were `TRE0001` in rule text and `TRE0014` in JSON and YAML. A repeated argument in
  a YAML `args` mapping is `DuplicateArgument` (`TRE0032`) like the other formats; it was a `TRE0014` YAML syntax error.
  `MalformedTree` (`TRE0014`) is now only for the shape of a JSON or YAML tree. No code was renumbered. Migration: match
  on the new code where you matched on the old one. The code table in `docs/strong-k3/specification/diagnostics.md` lists
  every code with its severity, phase and default, and a test fails when a code in `DiagnosticCodes` is missing from it.
- Breaking: `EqualsConfigurable` and `NotEqualsConfigurable` in `StringPredicates` are case-sensitive by default. The
  `ignoreCase` argument defaults to `false` (it was `true`), so every string comparison in the catalog is ordinal and
  case-sensitive unless the rule opts in. Migration: add `ignoreCase: true` to each `EqualsConfigurable` or
  `NotEqualsConfigurable` call that relied on the old default.
- Every `NotX` twin in `TruthWeaver.Predicates` is the strict Strong Kleene complement of its positive predicate for a null
  selected value too. Under `NullBehavior.False` the positive predicate answers `False` and its twin now answers `True`
  (the `StringPredicates`, `RegexPredicates`, `NumericPredicates` and `ScalarPredicates` twins answered `False` before).
  This covers `NotEqual`, `NotContains`, `IsNotEmpty`, `NotMatches`, the numeric and scalar `NotEqual`, `NotIn`,
  `Outside` and `IsNotDefault`, and `GreaterThanOrEqual` and `LessThanOrEqual` (the twins of `LessThan` and
  `GreaterThan`). Under `NullBehavior.Unknown` both members still answer `Unknown`. Positive predicates and the definite
  null tests are unchanged.
- Breaking: every built-in predicate in `TruthWeaver.Predicates` answers `Unknown` for a null selected value by default
  (`NullBehavior.Unknown`), because it cannot evaluate a missing value. `Equals`, `EqualsIgnoreCase`, `StartsWith`,
  `EndsWith`, `Contains` and `EqualsConfigurable` in `StringPredicates`, `RegexPredicates.Matches` and
  `CollectionPredicates.SetEquals` defaulted to `NullBehavior.False` and answered `False` (`SetEquals` read a null
  collection as empty, so it answered `True` for an empty argument array). Their twins `NotEqual`, `NotEqualsIgnoreCase`,
  `NotStartsWith`, `NotEndsWith`, `NotContains`, `NotEqualsConfigurable`, `NotMatches` and `NotSetEquals` default to
  `NullBehavior.Unknown` too, so both members of a pair registered with the defaults answer `Unknown`. Migration: pass
  `nullBehavior: NullBehavior.False` at registration to keep the earlier answer; the twin then answers `True`, and
  `SetEquals` keeps the empty-set reading. The definite null tests are unchanged.
- `DateTimePredicates.Between` and `Outside` throw the same reversed-bounds `ArgumentException` as the numeric range
  predicates: `ParamName` is `args` (it was `lower`) and the message is
  `Predicate '<name>' has reversed bounds: 'lower' (<lower>) is greater than 'upper' (<upper>).`, with both bounds in the
  round-trip `O` format of the invariant culture. The `lower` and `upper` argument descriptions are now
  `The inclusive lower bound (date-time).` and `The inclusive upper bound (date-time). It must not be less than the lower
  bound.`

- Breaking: `IDataSource.ScopeAsync` returns `ValueTask<DataScopeResult>` instead of `ValueTask<IDataSource>`. A scope query that is
  malformed, matches no node, matches several nodes or reaches an unsupported node is a failure result, no longer an
  `ArgumentException` or `InvalidOperationException`. Migration: read `result.Source` when `result.Succeeded`, otherwise
  `result.ErrorKind` and `result.ErrorMessage`; an implementation returns `DataScopeResult.Success(source)` or
  `DataScopeResult.Failure(kind, message)`. `FakeDataSource` returns a `NoMatch` failure for a scope nobody scripted (it threw
  before). [ADR-0006](docs/adr/0006-data-sources-for-expression-variables.md) decision 8 is amended in place.
- Every break below. Each one has a migration step in the sections that follow.
- `CompiledRule<TContext>.EvaluateAsync` is one method: `EvaluateAsync(context, services = null, dataSources = null, options = null, cancellationToken = default)`.
  It replaces the two overloads. A caller supplies only what the rule needs, and a null `services` is an empty provider
  (a class-based predicate yields `Unknown` plus a `Fault`). Parameter order: `services` stays second and `dataSources`
  third, so existing `(context, services, dataSources, ...)` calls and all `cancellationToken:` calls compile unchanged.
  A call that passed `options` as the third positional argument must name it: `EvaluateAsync(context, services, options: options)`.

### Fixed

- `RuleBuilder.Predicate` accepts `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` (and any other `IEnumerable` of
  supported values) as an array-valued argument. Before, these value-type arrays threw `ArgumentException`.

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
- **New:** comparison is ordinal; the arguments are `value`, `ignoreCase` (default `false`) and `trim` (default `false`).
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
