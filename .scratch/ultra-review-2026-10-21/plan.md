# Ultra review plan - 2026-10-21 (bounded)

Status: phases 2-4 done on 2026-10-21 (results in `summary.md` and `issues/`); phase 1 (K3 oracle) not yet written

Run notes:

- Phase 2 and 3 ran before the oracle was written, so the reviewers did not get oracle mismatches as leads. Oracle
  mismatches will be filed as new tickets.
- The phase 0 baseline was only partly run: the pre-commit hook's build and test run passed except
  `SkillProvenanceTests` (the untracked `.claude/skills/voice-of-robert` has no row in `.claude/skills/README.md`).
  The package vulnerability and outdated checks were not run.
- 14 reviewers ran: 9 folder slices, 4 lenses, 1 surface pass.

## Goal

Review the critical paths of `src/` at `main` (e0ee3d7) and turn each finding into a ticket under
`.scratch/ultra-review-2026-10-21/issues/`. Strong Kleene (K3) correctness and logical consistency come first.
Nothing is fixed during the review. Fixes follow later as dispatched tickets (`/dispatch-tasks`).

## Scope

Measured 2026-10-21, source lines only.

| Tier | Folders | Lines | Depth |
| --- | --- | --- | --- |
| 1: K3 core | `TruthWeaver/{Ast,Parsing,Compilation,Evaluation,Analysis,Rewriting}`, `TruthWeaver.Abstractions`, `TruthWeaver.Predicates` | ~28k | Full |
| 2: Same semantics, other surfaces | `TruthWeaver/{Printing,Json,Building,Registry,Diagnostics}`, `TruthWeaver.Yaml` | ~10.5k | One consistency pass |
| 1b: Predicate and data source concepts | `TruthWeaver/Registry`, `Abstractions/{IDataSource,DataSources}`, `Compilation/DataSourceDeclarations`, `Building/DataSourceExtensions`, `DataSources.Json`, `Yaml/YamlDataSource`, `Generators` (predicate source generator) | ~3.5k | Full |
| 1c: Policy combining | `TruthWeaver.Policies` | ~0.5k | Full |
| 3: Peripheral | Diffing, Metrics, AspNetCore, Testing | only the lines that surface True, False or Unknown (lens L5) | Targeted |

Not reviewed, by decision: public API design and XML docs, packaging and trim/AOT claims, `Extensions.Configuration`,
`Diffing`, `Metrics`, `Logging`, dependency injection.

Out of scope: `Tool`, `Tool.Core`, `Tool.Tui`, all `tests/`, `samples/`, `benchmarks/`, the `Testing` fakes
(`FakeDataSource`, `FakePredicates`). Exception: the K3 oracle (below) adds one test class.

Operator coverage is every operator, with no exceptions. The closed set in `OperatorDefinitions` has 25
definitions. The reviewers also cover what is not in that table: constants, `ForAll`/`Exists` quantifiers,
definitions and parameters, notes, and the `Decision` methods `Project` and `Collapse`. Phase 0 checks the list
against `OperatorDefinitions`, the compiler and `docs/strong-k3/` and files a ticket for any operator that one of
them lacks.

## Already reviewed (do not repeat)

Phase 0 reads these. A finding already recorded there, or covered by an open ticket, is linked, not re-filed.

- `.scratch/main-review-2026-10-17/`, `.scratch/architecture-review-2026-10-19/`
- `.scratch/k3-hardening/`, `.scratch/k3-followups/`, `.scratch/test-coverage-gaps/`
- `.scratch/repo-improvements-2026-10-17/`, `.scratch/followups-2026-10-11/`
- Not sources of truth: `.scratch/k3-conformance/_superseded/`, `.scratch/engine-v1`. `.tmp/` is reference only.
- Authority for semantics: ADR-0001, ADR-0002, ADR-0005, `CONTEXT.md`, `docs/strong-k3/`.

## Phases

### Phase 0 - Baseline (no model tokens)

1. Run the validation list from `CLAUDE.md`: restore (locked), build with `CI=true`, test, both formatter checks,
   Roslynator.
2. `dotnet list package --vulnerable --include-transitive` and `--outdated`.
3. Build `known-issues.md` from the prior reviews above (one line each, with status).

Failing baseline items become tickets directly.

### Phase 1 - K3 oracle (I write it, no agents)

A new test class in `tests/TruthWeaver.Tests`, kept as a permanent regression guard. It follows the repo test
rules (naming `{Member}_{Scenario}_{Expectation}_Test`, `<summary>`, one behavior per test).

For every one of the 25 operators plus constants and quantifiers, over all True/False/Unknown inputs (arity 1 to
4, exhaustive, and the threshold operators for every K from 0 to n+1), compare an independent reference table
against:

1. the evaluator (`Evaluator`),
2. the analyzer and BDD (`Analyzer`, `BddManager`): tautology, contradiction and equivalence verdicts,
3. every rewrite (`Rewriting/`): the rewritten tree gives the same result as the original on all inputs,
4. print then reparse for each printer that has a parser (DSL, tree format, JSON, YAML): same tree, same result,
5. fault and Unknown propagation for predicates that throw, time out or return Unknown,
6. data source values: a missing key, a null, a wrong type and a present value, each against the behavior that
   ADR-0006 and the docs state (the JSON, YAML and in-memory sources must agree).

The reference table is written from the K3 definitions in `docs/strong-k3/`, not copied from the code. Every
mismatch becomes a critical ticket with its exact inputs, and is passed to the Phase 2 reviewers as a lead.
Operators that are not information-monotone (`COALESCE`, `Is*`) get their own expected tables.

### Phase 2 - Review (14 agents, parallel)

Read-only. Each agent cites `file:line` and a concrete failure scenario for every finding. Each reports "no
findings" for a clean slice. Each uses `codegraph_explore` before grep or file reads.

**Folder slices (9 agents, Opus 5.5).** Each reads its slice in full, with the related docs page.

| Slice | Content | Lines |
| --- | --- | --- |
| F1 | `Ast` + `Abstractions` (`TruthValue`, `Decision`, `IPredicate`, `NodeShape`, `OperatorDefinitions`) | ~3.1k |
| F2 | `Parsing` (`DslParser`, `TreeFormatReader`, tokenizer) | 3.7k |
| F3 | `Compilation` (`RuleNodeCompiler`, validation, definitions) | 2.8k |
| F4 | `Evaluation` (`Evaluator`, `Decision`, faults, traces, options) | 5.0k |
| F5 | `Analysis` + `Rewriting` (`Analyzer`, `BddManager`, rewrites) | 6.1k |
| F6 | `Predicates`, first half | ~3.5k |
| F7 | `Predicates`, second half | ~3.6k |
| F8 | Predicate and data source concepts (tier 1b above) | ~3.5k |
| F9 | `Policies` (every combining algorithm) | ~0.5k |

Extra focus areas, added to the slices named:

- **Resource limits (F2, F5).** Deep and wide rules against the parsers (stack depth, input size), the analyzer
  and `BddManager` (variable ordering and exponential blowup, any size or time limit and what it returns when it
  trips), the rewriters (fixed-point loops that may not end), and regex cost in string predicates (F6-F7).
- **Concurrency and cancellation (F4).** Is a compiled rule immutable and safe to evaluate from many threads?
  Shared mutable state, async paths, `FaultBudget` under parallel evaluation, `EvaluationOptions.Timeout`
  versus a predicate's own timeout versus caller cancellation (ADR-0001/0002).
- **Untrusted input parsing (Tier 2 pass).** `DecisionJsonReader`, `JsonTreeParser` and `YamlTreeParser` given
  malformed, hostile and oversized input: depth and size limits, duplicate keys, wrong types, YAML anchors and
  tags, and whether each failure is a diagnostic rather than an exception.
- **Policy combining (F9).** For each combining algorithm, the result for every mix of True, False and Unknown
  (and faults) from the member rules, whether it fails closed, and whether the docs state the same table.

Review focus per slice:

- F1-F5: K3 correctness, Unknown propagation, short-circuit and fault semantics (ADR-0001/0002), operator arity
  and edge cases, diagnostics, error handling without exceptions, comment accuracy, API design of public types.
- F6-F7 (K3 contract first): null or missing input gives Unknown (not False), exceptions give Unknown plus a
  Fault, timeouts and cancellation follow ADR-0001/0002, culture and time-zone independence, regex cost limits,
  argument validation. General code quality is secondary.

**Operator-family lenses (3 agents, Fable 5.1).** Each follows one family through every layer: parser, tree
format, compiler, evaluator, analyzer and BDD, each rewrite, `NodeShape`, `OperatorDefinitions`, builder, printers,
JSON and YAML schema, docs page. Looks for drift between layers. Together they cover all 25 operators.

| Lens | Operators |
| --- | --- |
| L1 (9) | `Not`, `And`, `Or`, `Xor`, `Equivalent`, `Implies`, `Nand`, `Nor`, `Parity` |
| L2 (10) | `Any`, `All`, `None`, `ExactlyOne`, `AtLeast`, `AtMost`, `GreaterThan`, `LessThan`, `Exactly`, `Between`, and the `ForAll`/`Exists` quantifiers |
| L3 (6) | `Coalesce`, `If`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`, and constants, definitions, parameters and notes |

**Least-surprise lens (L5, 1 agent, Fable 5.1).** Checks that True, False and Unknown behave the way a user
would expect everywhere they surface. Each finding states what a user would expect, what happens, and where the
two differ. Questions it asks:

- `TruthValue` itself: the default value, conversions to and from `bool`, equality and ordering, `ToString` and
  parsing, and whether anything makes Unknown behave like False by accident (or the reverse).
- `Decision`: `IsSatisfied` fail-closed, `Project` and `Collapse`, what a fault does to the result, and how the
  explainer and trace describe Unknown.
- Spellings of a literal in the DSL, JSON and YAML (`true`, `True`, `"unknown"`, `null`, a missing value): which
  are accepted, which are rejected, and whether the diagnostic says what to write instead.
- Predicates and data sources: a missing or null value gives Unknown (not False or an exception), the same in
  every source, and the docs say so.
- Consumers: what `Policies` and `AspNetCore` do with an Unknown result (allow, deny, or a distinct outcome), and
  whether that matches `IsSatisfied` and the docs.
- The words in diagnostics, printers and docs: "unknown", "not evaluated", "faulted", "skipped" are used for one
  thing each.

L4 is the predicate and data source concept, covered by slice F8 (Opus 5.5) rather than a separate lens.

**Tier 2 pass (1 agent, Opus 5.5).** Checks that `Printing`, `Json`, `Yaml`, `Building` and `Registry` agree with
the parser and evaluator: operator names and labels, precedence and parenthesization, escaping, schema
(`rule-tree.schema.json`), round trips, and `docs/strong-k3/` sync. Not a line-by-line review.

### Phase 3 - Verification

A fresh Sonnet 5.5 agent gets each critical or high finding and the code only, not the reviewer's reasoning. It
reproduces or refutes the finding (a failing test, a `dotnet` command, or a traced path). Survivors are marked
`CONFIRMED`. Refuted findings go to `rejected.md` with the reason. Medium and low findings are filed marked
`PLAUSIBLE` without verification.

### Phase 4 - Synthesis and tickets (me)

1. Deduplicate across slices, lenses, the oracle and the known-issues index.
2. Group by theme where one fix covers several findings.
3. Write one ticket per file to `issues/NN-<slug>.md`, numbered from `01`.
4. Write `summary.md`: counts by severity and layer, top risks, suggested fix order, dispatch batches.

## Ticket format

Follows `docs/agents/issue-tracker.md`.

```text
# NN - <title>
Status: needs-triage
Severity: critical | high | medium | low
Lens: <slice, lens or oracle>
Verdict: CONFIRMED | PLAUSIBLE
Blocked by: <NN, NN or none>

## Finding
<one paragraph, with file:line evidence>

## Failure scenario
<concrete input or state and the wrong result>

## Acceptance criteria
- <observable outcome>
- Updates <doc page> (required when behavior or public API changes)
- Validation list from CLAUDE.md passes
```

Severity: **critical** is a wrong K3 result, a layer disagreement, data loss or a security hole. **high** is a
defect with a realistic trigger. **medium** is a design or test gap with a likely future cost. **low** is
cleanup. Findings that CSharpier or the analyzers already enforce are not filed.

## Cost estimate

Based on API list prices (cached 2026-10-06): Opus 5.5 $4 in / $20 out, Sonnet 5.5 $2 in / $10 out,
Fable 5.1 $10 in / $50 out per MTok. Token counts are my estimates (about 12 tokens per C# line), not
measurements.

| Item | Agents | Model | Estimate |
| --- | --- | --- | --- |
| Folder slices F1-F9 | 9 | Opus 5.5 | $27-52 |
| Tier 2 pass | 1 | Opus 5.5 | $5-10 |
| Operator lenses L1-L3 | 3 | Fable 5.1 | $24-45 |
| Least-surprise lens L5 | 1 | Fable 5.1 | $8-15 |
| Verification | 20-40 | Sonnet 5.5 | $6-15 |
| Oracle test and synthesis | me | session model | $6-12 |
| **Total** | | | **about $80-155** |

Running TruthWeaver itself has no service cost: it is a library and a CLI. CI minutes and NuGet hosting are
free or near free for a public repository. I could not verify visibility from the repo.

On a subscription instead of the API, cost is usage-limit use, not dollars.

Wall clock: Phase 2 about 30-45 minutes in parallel, verification about 15-25 minutes.

## Running it

- The reviewers run as a workflow. That needs the owner's explicit opt-in at launch.
- The free `/code-review ultra` runs (3 for Pro and Max, one time) are better spent later on the fix branches,
  as an independent diff review. They cannot take a whole-repo snapshot (default limits: 500 files, 8,000
  lines).
