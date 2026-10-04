# Strong Kleene (K3) reference: validation report

The Phase 8 validation of the [reference](README.md), run on 2026-10-03 against branch `StrongK3+Operations` at commit `1382a39` (tickets 01 to 10 done). It records what was run, what passed, what was checked by hand, and every gap or open question. Nothing here was resolved silently: where a choice is open it is listed under [Unresolved questions](#unresolved-questions) with a recommendation.

> [!NOTE]
> This report describes the reference as it stood after ticket 10, refreshed for [ticket 12](../../.scratch/k3-reference/issues/12-root-navigation-polish.md) (navigation polish): U6 is resolved and the repository `README.md` and `CONTEXT.md` now link to the reference. Ticket 14 (keep the reference in sync) is still open and will change some findings below.

## Summary

| Area | Result |
| --- | --- |
| Verification harness (`K3ReferenceTests`) | Pass: 16 of 16 |
| Full test suite | Pass: 2222 of 2222, 0 failed, 0 skipped |
| Repository validation (restore, build, test, CSharpier, format, Roslynator) | Pass: all six steps, 0 warnings, 0 diagnostics |
| Documentation checks | 6 pass, 3 pass with stated limits, 1 pass with findings (see [Documentation checks](#documentation-checks)) |
| Independent probe of the real library | 49 compile probes and 7 evaluation probes: every probed documentation claim held; 2 discrepancies found in the repository README, none in the reference |
| Unresolved questions | 12, each with a recommendation |

## What was run

All commands ran from the repository root with `CI=true`, so every warning counts as a failure. The tree was clean at the start.

| Step | Result | Evidence |
| --- | --- | --- |
| `dotnet restore --locked-mode` | Pass | 12 projects restored, exit 0 |
| `dotnet build` | Pass | `Build succeeded.`, 0 warnings, 0 errors |
| `dotnet test` | Pass | 2222 tests: `Tests` 2003, `Abstractions.Tests` 96, `Predicates.Tests` 70, `Testing.Tests` 32, `Yaml.Tests` 12, `Architecture.Tests` 9; 0 failed |
| `dotnet csharpier check .` | Pass | `Checked 268 files`, exit 0 |
| `dotnet format --verify-no-changes --severity info` | Pass | exit 0 |
| `dotnet roslynator analyze` | Pass | `0 diagnostics found` |

The harness alone, `dotnet test tests/TruthWeaver.Tests --filter-class "*K3ReferenceTests"`, passes 16 of 16. It includes `CheckTree_RealReference_ReportsNoFailures_Test`, which checks every Markdown file under `docs/strong-k3/`, and 14 tests that feed the checker deliberately wrong fixtures (a wrong cell, a missing row, a wrong canonical form, a form that only holds for two operands, broken links and anchors, a wrong Kind or category, an unknown operation, a missing marker) and require a `file:line` failure.

### What the harness verifies

| Check | Count in the reference | Verified against |
| --- | --- | --- |
| `k3:truth` tables (every one of the 3^n assignments present once and correct) | 35 | The independent `K3Oracle`, not the engine |
| `k3:eval` tables (every pair of definitely-true and possibly-true counts) | 40 | `K3Oracle` |
| `k3:canonical` forms (every assignment, operand count and valid parameter) | 101 | `K3Oracle` |
| Operation documents (27): in the approved inventory, right directory, right Kind, exactly one `Category:` line, ten required sections present and non-empty, a marker in each Truth Table, Evaluation Table and Canonical Form section | 27 of 27 | The inventory in [PROPOSAL.md](PROPOSAL.md) section 4 |
| Relative links and `#heading` anchors in every `.md` under `docs/strong-k3/` | all | The file system |

### What the harness does not verify

These are covered only by the manual review and probes below, or not at all:

- The LaTeX in Formula sections (it is not parsed; agreement with the tables is by review).
- Mermaid diagrams (neither parsed nor rendered).
- Alias lists, diagnostic codes and messages, arity prose, edge-case prose, and the statements in Equivalent Forms and Implementation Notes that carry no marker.
- Links from the repository `README.md` and `CONTEXT.md` into the reference (added by ticket 12; the harness checks only files under `docs/strong-k3/`, so these two links are checked by hand).

## Documentation checks

| # | Check | Result | Evidence |
| --- | --- | --- | --- |
| 1 | Every operation has exactly one primary category | Pass | 27 documents, each with one `Category:` line equal to its directory's label; enforced by the harness |
| 2 | Arity, Input Domain and Output Domain present | Pass | The three sections are required and non-empty in all 27 documents; the arity statements were probed against the compiler for the gates, derived operators, cardinality operations, `If` and the inspections ([probe results](#independent-probe-of-the-real-library)) |
| 3 | Kind explicit and consistent | Pass | Every document starts its Kind section with `Primitive` or `Derived`, matching the inventory: 7 Primitive plus `Collapse`, the rest Derived (the 7 primitives in PROPOSAL section 1.4 are `NOT`, `AND`, `OR`, `AtLeast`, `AtMost`, `Exactly`, `COALESCE`) |
| 4 | Formulas agree with the tables | Pass with limits | The tables are machine-checked; the LaTeX is not. The Formula sections of `XOR`, `EQUIVALENT`, `If` and `Collapse` were read against their tables and agree. The other formulas were not individually re-derived |
| 5 | Canonical forms valid | Pass | 101 forms checked over all assignments, the operand counts each marker declares, and every valid parameter |
| 6 | Mermaid agrees with the formula | Pass with limits | 5 operation diagrams (`XOR`, `EQUIVALENT`, `If`, `Project`, `Collapse`) were compared with their formulas by hand and match, for example the `XOR` graph is `OR(AND(a, NOT b), AND(NOT a, b))`. They were **not rendered or syntax-checked by a tool**: the Mermaid Chart connector needs interactive authorisation that this session cannot give. The 2 diagrams in `specification/values.md` show the two orders and have no formula |
| 7 | Aliases unambiguous | Pass with limits | No spelling is listed as an alias in more than one document. Probed: `XNOR` and `IFF` compile to `EQUIVALENT`, `??` to `COALESCE`, `c ? t : f` to `If`, `NXOR` is rejected with its rename message. Not harness-checked |
| 8 | Links valid | Pass | Harness, all relative links and anchors |
| 9 | No operation documented inconsistently | Pass | All 27 inventory operations have exactly one document; no stray documents; the contrasts table in `functions/if.md` (bare multiplexer 1 triple, McCarthy 2, SQL `CASE` 4) was recomputed by hand and is right |
| 10 | Terminology consistent | Pass with findings | One naming mismatch and one stale note, see U6; the "gate" label conflicts with `CONTEXT.md`, see U3 |

## Independent probe of the real library

To check claims the harness cannot, a throwaway xunit test (not committed, deleted afterwards) compiled 49 rule texts through `RuleCompiler<T>.Compile` and evaluated 7 rules against the real engine. Predicates `a`, `b`, `c`, `x`, `y` answered `Unknown`, `isOn` `True`, `isOff` `False` and `boom` threw.

| Claim in the reference | Probe | Result |
| --- | --- | --- |
| `a XOR b XOR c` is `BRE0006` pointing at `PARITY` and `ExactlyOne` | compile | Confirmed, message matches |
| `XOR(a, b)`, `AND(a, b, c)`, `NAND(a, b)`, `OR(a)` have no call form (`BRE0001`) | compile | Confirmed |
| `a AND b AND c` is one flat three-operand node | compile, print | Confirmed |
| `NXOR` is rejected, `XNOR` and `IFF` are accepted as `EQUIVALENT` | compile | Confirmed |
| `PARITY(a)`, `ExactlyOne(a)`, `ANY(a)`, `ALL(a)`, `NONE(a)`, `COALESCE(a)` need two operands (`BRE0014`) | compile | Confirmed |
| `AtLeast(3, a, b)` is `BRE0008` with the quoted message | compile | Confirmed, message identical |
| `AtLeast(1)` is `BRE0014` "requires at least one operand"; `AtLeast(a, b)` is `BRE0001` | compile | Confirmed |
| `AtLeast(1, a)`, `Exactly(1, a)`, `GreaterThan(0, a)`, `LessThan(1, a)` compile with one operand | compile | Confirmed (see U1) |
| `AtMost(1, a)` is rejected because `k` must be `0..n-1` | compile | Confirmed, `BRE0008` |
| `BETWEEN(0, 2, a, b)` and `BETWEEN(1, 3, a, b)` are `BRE0008`, `BETWEEN(1, 1, a)` is `BRE0014` | compile | Confirmed, messages identical |
| `If` with 2 or 4 operands is `BRE0014`; `a AND b ? x : y` and `a ? b ? x : y : z` are `BRE0007`; `(a AND b) ? x : y` compiles; `if(a, b, c)` is accepted | compile | Confirmed |
| `IsTrue(a, b)` is a one-operand error; `Collapse(...)` and `Project(...)` are `BRE0001` with the pointer to the `Decision` methods | compile | Confirmed |
| `If(boom, isOn, isOn)` is `True` with 1 fault | evaluate | Confirmed |
| `If(boom, isOn, isOff)` is `Unknown`, 1 fault | evaluate | Confirmed |
| `If(isOn, isOff, boom)` is `False`, 0 faults (branch not run) | evaluate | Confirmed |
| `If(isOn, boom, isOff)` is `Unknown`, 1 fault | evaluate | Confirmed |
| `IsTrue(boom)` is `False` and records a fault; `boom ?? True` is `True` and records a fault | evaluate | Confirmed |
| `Project(false)` and `Collapse(UnknownIsError)` on those results (`False`/`RejectedUnresolved` for an `Unknown`) | evaluate | Confirmed; `IsSatisfied` is `True` only for `True` |

Two further discrepancies came out of the probe, both outside the reference:

- `README.md` line 559 says `AND` and the other n-ary operators are written like `AND(a, b, c)`. The DSL rejects every call form of an infix operator (`BRE0001`). See U2.
- `OperatorDefinitions` records a minimum of 2 operands for `AtLeast`, `AtMost`, `Exactly`, `GreaterThan` and `LessThan`, while the compiler accepts 1. See U1.

## Missing operations, categories and ambiguities

- **Operations.** None are missing from the final operator set in [ADR-0005](../adr/0005-strong-k3-language-surface.md): 27 inventory operations, 27 documents. Predicates are intentionally absent (ticket 13, on hold). The literals `True`, `False` and `Unknown`, terms, and the infix spellings are not Operations by the approved decision (PROPOSAL question 8) and are documented as syntax.
- **Categories.** None missing. The `predicates/` directory is a placeholder index only.
- **Ambiguous classifications**, all decided by the owner on 2026-10-03 but still worth a reader's attention: `PARITY` sits with the derived connectives although it is defined through `Exactly`; `If` is a Strong Kleene connective filed under Functions; the four inspections are Derived and external; `Collapse` is Primitive and `Project` Derived although neither is a rule node. See U8 and U9.
- **Unverified equivalences and decisions in the underlying specification.** The NAND-alone and NOR-alone expressibility claims, the statement that the no-tautology theorem does not extend to the external operators, and the simplifier statements in Implementation Notes are prose. They rest on the brute-force pass recorded in PROPOSAL section 5 and on [ADR-0005](../adr/0005-strong-k3-language-surface.md) decision 10, not on a marker, so the harness would not catch a regression in them.

## Unresolved questions

Each item gives the finding, the options and a recommendation. Items with an owner or ticket reference are already tracked elsewhere.

| # | Question | Where tracked | Recommended resolution |
| --- | --- | --- | --- |
| U1 | **Threshold arity mismatch.** `OperatorDefinitions` says `AtLeast`, `AtMost`, `Exactly`, `GreaterThan` and `LessThan` take a minimum of 2 operands; `RuleNodeCompiler.BuildThreshold` accepts 1 (probe: `AtLeast(1, a)` compiles) and `README.md` line 559 repeats the "2 or more" claim. The reference documents the compiler (1 or more). | PROPOSAL question 10 (accepted A); [k3-hardening 11](../../.scratch/k3-hardening/issues/11-consolidate-duplicated-operator-logic.md) | Decide which side is intended. If one operand is wanted, change the table (and README row); if two, change the compiler and the 5 documents. Until then the reference follows the compiler. |
| U2 | **README `AND(a, b, c)` call form.** `README.md` line 559 shows the n-ary row as `AND(a, b, c)`, but the DSL has no call form for `AND`; the working spelling is `a AND b AND c`. The reference states this correctly in `gates/and.md`. | Not yet tracked | Add a small docs ticket to fix the README row (and its `AtLeast` minimum, U1). Not changed here. |
| U3 | **"Gate" versus `CONTEXT.md`.** Still open: ticket 12 added the `CONTEXT.md` link only and did not touch the wording. `CONTEXT.md` says an Operator is "never called a gate" (line 34) and that "gate" is circuit vocabulary (line 50). The reference category is "Gates / Operators" and `specification/terminology.md` defines "Gate" as reference vocabulary only. | PROPOSAL question 2 (accepted A) | Keep the owner's label, and in ticket 12 add the same exception sentence to `CONTEXT.md` so the two agree. |
| U4 | **`Category:` and `Category index:` lines.** Every document has both lines in Classification. The harness counts only `Category:`, so the second line is a convention the checker does not know and a later edit could drop it unnoticed. | Not tracked | Keep both; record the convention in `docs/doc-examples.md` and have the checker require the `Category index:` link to resolve to the directory's `README.md`. |
| U5 | **Stale ADR-0005 span statement.** [ADR-0005](../adr/0005-strong-k3-language-surface.md) line 254 still says JSON diagnostics keep no positions; `CompileJson(string)` now reports spans. | [k3-hardening 12](../../.scratch/k3-hardening/issues/12-amend-adr-0005-json-span-statement.md) | Leave the ADR as is until that ticket amends it in place. The reference makes no span claim, so it is unaffected. |
| U6 | **Root README is stale.** Resolved by ticket 12: the "being added / pending" note is replaced and the navigation labels the category "Derived Logical Operations", as the directory, documents and checker do. | [ticket 12](../../.scratch/k3-reference/issues/12-root-navigation-polish.md) | Resolved. |
| U7 | **Fate of PROPOSAL.md.** Several pages link to it (root README, terminology, `functions/if.md`, `docs/doc-examples.md`) and the harness cites it by name. | PROPOSAL question 11 (accepted A: fold into terminology and delete at ticket 12) | Do it in ticket 12, but only after rewriting those links and the checker's message strings; otherwise keep it as the permanent design record (option B). |
| U8 | **`If` category.** `functions/if.md` still carries a note that the Functions placement stays on this list. It is a connective filed with Functions because it is ternary and value-selecting. | PROPOSAL question 5 (accepted A) | Keep Functions and drop the note once ticket 12 confirms. |
| U9 | **Kind of non-rule-node operations.** `Project` is Derived and `Collapse` is Primitive only by convention, since both are `Decision` methods outside the rule tree. | PROPOSAL question 6 (accepted A) | Keep. The documents already say "result transformation, not a rule node". |
| U10 | **Mermaid diagrams are not machine-verified.** The harness neither parses nor renders them, and this session could not render them either. | Not tracked | Render all 8 diagrams once with the Mermaid tool in an interactive session, and consider a harness check that each diagram's edges match its `k3:canonical` form. |
| U11 | **Harness coverage gaps.** Aliases, diagnostic codes and messages, arity prose, LaTeX formulas and unmarked equivalence claims are unchecked, so a code change can silently outdate them. | [ticket 14](../../.scratch/k3-reference/issues/14-keep-reference-in-sync.md) | Make ticket 14 cover at least aliases and the arity table (derive both from `OperatorDefinitions`) and a diagnostics-code check. |
| U12 | **Predicates and terms are undocumented by design.** The index is a placeholder; nothing in the reference can be used to document predicate semantics yet. | Ticket 13 (deferred) | Leave on hold until the predicates are implemented. |

## Result

The reference is internally consistent and agrees with the independent oracle for every table, evaluation table and canonical form it contains, and every claim sampled against the real library held. The open items are naming and bookkeeping (U3, U4, U6, U7, U8), two code-versus-document questions that need an owner decision (U1, U2), and verification gaps that tickets 12 and 14 are positioned to close (U10, U11). No reference document was changed by this validation.
