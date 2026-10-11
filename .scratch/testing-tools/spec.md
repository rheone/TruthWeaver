# Testing tools: predicate harness, equivalence assertion and public rule fuzzer

**Status:** done

Source: [library-roadmap](../library-roadmap/spec.md) re-score (2026-10-03), grilled 2026-10-09.

## Problem Statement

Consumers who write predicates or refactor rules have no supported way to check their work. A predicate author cannot
verify that a real predicate is deterministic, schema-conformant and robust at argument boundaries. A rule author cannot
assert that two rules mean the same thing without the internal `RuleEquivalence` call, and cannot fuzz a registry
against the K3 oracle that the repository's own tests use.

## Solution

Three additions to `TruthWeaver.Testing`, each its own ticket:

1. `AssertEquivalent(ruleA, ruleB)`, a thin assertion over `RuleEquivalence.Compare`.
2. `PredicateHarness`, which checks a real predicate and returns a report.
3. A public `RuleFuzzer`, moved out of the test project, with the K3 oracle checks built in.

## Decisions

- **Packaging.** `TruthWeaver.Testing` takes a project reference to `TruthWeaver`. This reverses the current rule that
  `Testing` depends on `Abstractions` alone. The package-boundary architecture test
  (`PackageBoundaryTests`, `Testing_depends_on_abstractions_alone_...`) and ADR-0004 change first, in the prefactor
  ticket. The decision needs an ADR amendment, recorded in ADR-0004 in place.
- **Equivalence assertion.** `NotEquivalent` throws and prints the `TruthValue` counter-example over the union of terms.
  `Undecided` (term cap `MaxAnalysisTerms` exceeded) also throws, as "inconclusive": a test must not pass on an unproven
  claim. The message names the cap and says to raise it or shrink the rules.
- **Predicate harness report.** `RunAsync` returns a `PredicateHarnessReport` with one outcome per check
  (passed, failed with a reason, or observed). A `ShouldPass()` helper throws a harness exception for test runners.
  Checks: determinism (same arguments and context give the same answer), boundary values per declared `LiteralKind`,
  schema conformance (every declared argument is read, no undeclared argument is read), and cancellation.
  Cancellation is observed and reported, never enforced.
- **Thrown exceptions.** Any exception from a generated boundary value is a finding, because the engine turns it into
  `Unknown` plus a `Fault` (ADR-0001). The author may allow-list expected exceptions, per argument or per exception
  type (for example `ArgumentException` for reversed `Between` bounds). An allow-listed exception reports as
  "expected fault", not as a failure.
- **Unknown is valid.** The harness treats an `Unknown` result as valid and reports whether it came with a fault.
- **Fuzzer.** `RuleFuzzer` takes a `PredicateRegistry` and a seed, generates valid rules over its predicates, and runs
  the oracle checks: the evaluator matches brute-force Strong Kleene, `Simplify` and `Canonicalize` preserve meaning,
  and the DSL and JSON forms round-trip. A failure reports the seed and the rule text. It generalises `K3RuleGenerator`
  and `K3Oracle` from `tests/TruthWeaver.Tests/TestSupport`, decoupled from the test predicates.

## Out of scope

- Fuzzing argument values against one predicate beyond the harness boundary set.
- A consumer-supplied property hook (FsCheck style).
- Sampling fallback for `Undecided`.
- Enforcing cancellation.

## Further notes

- Each ticket that adds public API carries XML docs, tests named per CLAUDE.md, and the full validation set.
- No operation or predicate is added, so the K3 reference sync does not apply.
