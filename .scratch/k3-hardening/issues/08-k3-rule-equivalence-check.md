# 08: K3-aware rule equivalence check

**What to build:** A public way to ask whether two rules are equivalent under Strong K3, built on the dual-rail analyzer and canonicalisation (roadmap item 'public rule-equivalence check'). Return equivalent, not equivalent (with a counter-example assignment of True/False/Unknown to the terms) or undecidable within the term cap, never throwing for normal input. Extend rule diff to report whether a structural change preserves meaning. Verify with the oracle over generated rule pairs.

**Blocked by:** 03

**Status:** done

- [x] Equivalent and non-equivalent pairs are classified correctly over generated rules against the oracle
- [x] A counter-example assignment is returned for non-equivalent rules
- [x] The term cap is respected with a clear 'cannot decide' result
- [x] README documents the API and its limits
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- API (approved shape): `RuleEquivalence.Compare<TContext>(first, second, CompilerOptions? options = null)` in `TruthWeaver.Analysis` returns `RuleEquivalenceResult(Outcome, CounterExample, Reason)`; `Outcome` is `Equivalent`, `NotEquivalent` or `Undecided`. The counter-example is a string-keyed `TruthValue` map over the union of both rules' terms (keyed by printed term; terms the difference does not depend on read `False`). The cap reuses `MaxAnalysisTerms` over the union of distinct terms. `RuleDiffResult` gained `bool? PreservesMeaning` (second, optional positional parameter; `null` = undecided), computed by `RuleDiff.Compare` with default options.
- Implementation: `Analyzer.FindDifference` builds both trees on one BDD, ORs the XOR of each rail, and `BddManager.FindSatisfyingAssignment` walks a path to a witness. Verified in `tests/TruthWeaver.Tests/RuleEquivalenceTests.cs` over 300 generated pairs (self, canonical form, unrelated rule) against `K3Oracle`, including that every counter-example really separates the rules. README section "Rule equivalence" documents the API and limits.
- Not done: `RuleDiff.Compare` takes no `CompilerOptions`, so a rule pair over the default 20 distinct terms gets `PreservesMeaning == null`; callers use `RuleEquivalence.Compare` with a larger cap.
