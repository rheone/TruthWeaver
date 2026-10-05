# ADR-0006: Data sources for expression variables

## Status

Accepted (2026-10-03). Supersedes the "literal arguments only" decision in
[ADR-0003](0003-rule-syntax-and-serialization.md) and the "remain deferred" recommendation of
`.scratch/context-bound-term-arguments`.

## Context

A term's arguments are rule-text literals ([ADR-0003](0003-rule-syntax-and-serialization.md)). Anything
that changes per request has to be read by a predicate from its own `TContext`, so a rule such as
"the user's age is at least the limit stored in this configuration document" needs a bespoke predicate for
every value. Hosts keep that data in JSON, YAML or elsewhere and want a rule argument to say "take this
value from over there" without writing a predicate per field.

ADR-0003 rejected a context path grammar for two reasons: it needs a real typed path language, and it breaks
structural rule equality. Both objections are answered below: the path language belongs to the data
source, not to TruthWeaver, and equality compares the reference text, not the resolved value.

## Decision

1. **A variable is an argument source.** Wherever a predicate argument takes a literal, a rule may instead
   give a *variable reference*: a source name plus a query string. The predicate and its schema are
   unchanged; the engine resolves the reference, checks it against the argument's `LiteralKind`, and passes
   the value on. A variable is not a predicate and not a new operator.
2. **Resolved at evaluation time.** Nothing is read while compiling. The compiled tree holds the reference;
   each evaluation resolves it. (Reading a value once while building a rule is a `RuleBuilder` convenience,
   decision 12, not a compiler feature.)
3. **Named sources are supplied per evaluation.** The caller passes a `DataSources` set (name to
   `IDataSource`) as an optional `EvaluateAsync` argument. A rule that uses variables evaluated without the
   source it names yields `Unknown` plus a `Fault`.
4. **Source names are declared at compile time, optionally with a query validator.** The compiler is given
   the set of source names; an unknown name is a diagnostic, like an unregistered predicate. The compiler never
   sees a source instance (sources arrive per evaluation), so a name may be declared with an `IQueryValidator`:
   a small stateless object whose `Validate(string query)` checks a query's syntax in that source's dialect and
   returns problems. A malformed query is then a compile diagnostic with a code, the path or span of the
   query string, and the validator's message. A name declared without a validator is not syntax-checked, and
   a malformed query surfaces at evaluation as a source-error fault. A validator checks syntax only; it cannot
   know whether the data holds a match.
5. **Each source owns its query dialect.** Core treats the query as an opaque string. The JSON and YAML
   sources use JSONPath (RFC 9535); YAML is read into the same data model as JSON.
6. **Cardinality.** A scalar argument needs exactly one match: zero matches (missing) and two or more
   (ambiguous) are faults, so the term is `Unknown`. A path or filter selects the node, for example
   `$.orders[?@.id=='A7'].total`. An array argument collects every match; zero matches always give an empty array,
   including a path to a missing property, because a query result cannot tell the two apart. A scalar query on
   the same path faults as missing, and a query validator catches mistyped syntax at compile time.
7. **Conversions follow the DSL literal rules and nothing wider.** A string parses to `DateTimeOffset` and
   `Guid`; a JSON integer widens to `Decimal`; a whole-number `Decimal` narrows to `Int64` only when exactly
   representable. A string is never coerced to a number or boolean, and a number is never coerced to a
   string. Anything else is a type-mismatch fault.
8. **Scoping.** Queries are absolute within a source. `IDataSource.ScopeAsync` returns a new source rooted at
   the single node a query matches, so a host can narrow one repeated subtree and evaluate against it.
   There are no relative (`@.`) queries in rule text and no iteration over repeated subtrees; both are
   out of scope. *(Amended 2026-10-04.)* `ScopeAsync` returns a `DataScopeResult`, not the source itself: a
   malformed query, a query that matches no node or several, and an unsupported node are failure results
   (`MalformedQuery`, `NoMatch`, `AmbiguousMatch`, `UnsupportedType`), never exceptions. The interface has no other
   error channel, async members cannot use `out`, and a scope query can come from external input.
9. **The interface.** `IDataSource` (in `TruthWeaver.Abstractions`) is async, converts each matched node to
   `LiteralValue`, and reports malformed queries and source failures as data in `DataQueryResult` (and, for scoping, `DataScopeResult`). The
   evaluator still catches a thrown exception as a backstop ([ADR-0001](0001-kleene-failure-model.md)).
10. **Syntax.** `from("user", "$.minAge")` in the DSL; `{ "from": "user", "query": "$.minAge" }` as an
    argument value in JSON and YAML; `Arg.From("user", "$.minAge")` in `RuleBuilder`. The query is an ordinary
    quoted string, so JSONPath characters never meet the DSL tokenizer. `from` is reserved as an argument
    value form.
11. **Term identity and memoization.** Two terms are the same variable only when the predicate, argument
    names and every `(source, query)` pair are identical. Different queries are different variables, even if
    the data would give equal values. Within one evaluation each `(source, query)` pair is queried at most
    once, and a fault is memoized like a value.
12. **Builder.** `RuleBuilder` accepts `Arg.From(...)` wherever it accepts an argument value (deferred), and
    offers a helper that reads a value from an `IDataSource` at build time and inserts the resulting literal
    (eager). The compiler and evaluator stay evaluation-time only.
13. **Trace and faults.** By default the trace and faults name the reference and the outcome (resolved,
    missing, ambiguous, type mismatch, source error, undeclared source) but never the resolved value. An
    opt-in (`EvaluationOptions.IncludeResolvedValues`) adds values to the trace. Faults never include values.
14. **Packages.** `IDataSource`, `IQueryValidator`, `DataQueryResult` and `DataSources` go in
    `TruthWeaver.Abstractions`. A new
    package `TruthWeaver.DataSources.Json` holds the JSON source, its JSONPath validator and the JSONPath
    dependency (Meziantou.Framework.JsonPath; see the amendment below). `TruthWeaver.Yaml` references it for the YAML source, which reuses that
    validator. `TruthWeaver` stays free of
    third-party packages other than the existing DI and logging abstractions. `TruthWeaver.Testing` gains a
    `FakeDataSource`.

## Considered options

- **Compile-time capture from the rule's own document.** Rejected as the core mechanism: the values a rule
  needs usually differ per request. The same effect is available by passing the document as a named source,
  or by the builder's eager helper.
- **One TruthWeaver path syntax for every source.** Rejected: it would be a weaker, privately maintained
  JSONPath, and it would still need a native escape hatch.
- **Our own JSONPath subset.** Rejected: RFC 9535 filters and functions are a large surface to own.
- **JSON source inside `TruthWeaver`.** Rejected: it would give core its first third-party dependency.
- **Variable as a predicate** (`Json("$.flag")` returning a truth value). Rejected as the primitive: it
  cannot feed values into other predicates. A thin predicate over one boolean variable can be added later.

## Consequences

- ADR-0003's statement that argument values are literals with no context-path syntax no longer holds. The
  canonical-equality argument survives: equality is structural on the reference text.
- Every layer that touches arguments changes: the DSL parser, `RuleNodeCompiler`, `Evaluator`, memoization,
  `NodeShape`, the printers, JSON and YAML, `rule-tree.schema.json`, the analyzer's term identity and
  `RuleBuilder`.
- A malformed query is a compile diagnostic when its source name was declared with a validator, and an
  evaluation-time fault otherwise. Validation is syntax only. `RuleBuilder` can run the same validator when
  given one.
- A source package that ships a dialect should ship its validator too, so hosts do not need to write one.
- A rule that resolves variables is only as deterministic as its sources. The predicate-author contract
  (same answer for the same term identity within one evaluation) is kept by the per-evaluation memoization
  in decision 11.
- Before the JSON package ships, its dependency's license and .NET 11 compatibility must be confirmed.

## Amendment (2026-10-04): the JSONPath engine

> **Amended in place.** Decision 14 first named JsonPath.Net, pinned at 2.2.0. The package is now Meziantou.Framework.JsonPath.

The engine must be free to use at every tier: no license, fee or EULA obligation for binaries or source.
JsonPath.Net 3.0.0 and later ship their binaries under the Open Source Maintenance Fee EULA, and 2.2.0 gets no updates.
Each candidate's license was read from its published `.nuspec` and `LICENSE` file and its repository.

| Candidate | License | RFC 9535 | Targets and dependencies | Maintenance | Verdict |
| --- | --- | --- | --- | --- | --- |
| **Meziantou.Framework.JsonPath 3.0.7** | MIT | Yes; tested against the JSONPath Compliance Test Suite | `net10.0` and `net11.0`; no dependencies; reads `JsonNode` directly | Active; commits to September 2026 | **Chosen** |
| Blazing.Json.JSONPath 1.1.0 | MIT | Claims full compliance with its own test suite | `net10.0` only; works on `JsonElement`, so every `JsonNode` needs a conversion | One author; first commit 2026-01-11, last 2026-01-12, 2 stars | Rejected: no activity since release and no `JsonNode` support |
| JsonCons.JsonPath 1.1.0 | Apache-2.0 | No; the .NET package follows the older JsonCons dialect | `netstandard2.1`; depends on `JsonCons.Utilities` and `System.Text.Json` 5.0.2; `JsonElement` only | Last push 2024-01-27 | Rejected: not RFC 9535 |
| Hyperbee.Json 3.3.2 | MIT | Yes | `net8.0` to `net10.0`; pulls `Microsoft.CodeAnalysis.CSharp.Scripting` and an expression compiler | Active | Rejected: heavy dependencies and no `net11.0` build |
| Corvus.Text.Json.JsonPath 5.7.5 | Apache-2.0 | Yes | `net10.0`; works on its own `Corvus.Text.Json` model | Active | Rejected: forces a second JSON object model |
| JsonPath.Net 3.0.2 | OSMF EULA (binaries) | Yes | `net10.0` | Active | Rejected: fee and EULA |
| JsonPath.Net 2.2.0 | MIT | Yes | `net10.0`, `netstandard2.0`; needs Json.More.Net | None | Replaced: no updates and a known `IndexOutOfRangeException` on a query ending in `.` |
| Own RFC 9535 subset | Ours | Partial | None | Ours | Rejected: filters and functions are a large surface to own |

Consequences of the swap:

- `JsonPaths` parses with `JsonPath.Parse` and turns its `FormatException` into a `QueryProblem`. The parser has no offset
  property, so the position is read from the "at position N" text in the message. `JsonPath.TryParse` is not used because it
  reports neither message nor offset. The `$.` workaround for JsonPath.Net 2.2.0 is gone.
- Filter expressions compare numbers as IEEE 754 doubles, so integers beyond 2^53 and some decimals can compare as equal
  inside a filter. Value extraction is unaffected: it reads the node's JSON text.
- `match()` and `search()` accept I-Regexp (RFC 9485) patterns only, as RFC 9535 requires; a pattern such as `\d` matches nothing.
- The trim and AOT analyzers (`IsAotCompatible`) report no warning for the package.
