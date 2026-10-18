# 21c: Rule set analysis (redundancy, shadowing, pairwise sweep)

**Parent:** [21](21-spike-overlap-and-conflict-analysis.md)

**What to build:** Set-level analysis built on 21b-2. Owner decisions (2026-10-16):

- Entry points take `IReadOnlyList<CompiledRule<TContext>>`, plus a thin immutable `RuleSet<TContext>` wrapper with no combining behavior yet.
- Rules are identified by their index in the input list.
- A `maxRules` cap (default about 50) gives a `TooManyRules` undecided-style result, not an exception.
- A new `RuleSetAnalysis` type, not methods on `RuleImplication`.
- Redundancy reports per rule plus one greedy removable set.
- An internal BDD node budget, also applied to ticket 13 and `RuleEquivalence`, returns `Undecided` with a reason.
- Assumptions between terms are an optional argument added after slice 1.
- Shadowing waits for ticket 07 to decide how an `Unknown` rule combines.

**Blocked by:** 21b-2, and 07 for shadowing only (the redundancy and pairwise-sweep parts could go first if the owner splits them).

**Status:** ready (owner decision 2026-10-17: approved; 21b-2 is done)

- [ ] Scope reconfirmed with the owner when ticket 07 is grilled
- [x] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

2026-10-18, redundancy and pairwise sweep built; shadowing not built.

- **Shadowing is out of this change.** It stays out until the owner reconfirms scope (first checkbox). Ticket 07 is now done, and ADR-0015 decision 5 says `FirstApplicable` never skips an `Unknown` policy. That matches the design note's "blocked by `True` or `Unknown`" reading, but the owner has not confirmed that this reading carries over to `RuleSetAnalysis`. Open question for the owner: build shadowing with that reading, take the reading as a parameter, or leave it out.
- **Public API** (`TruthWeaver.Analysis`): `RuleSet<TContext>` (implements `IReadOnlyList<CompiledRule<TContext>>`, copies its input, rejects a `null` rule), `RuleSetAnalysis.Analyze(IReadOnlyList<CompiledRule<TContext>>, int? maxAnalysisTerms = null, int? maxRules = null)` and the same with a `RuleSet<TContext>`, `RuleSetAnalysis.DefaultMaxRules` (50), `RuleSetAnalysisResult` (`Outcome`, `Pairs`, `Rules`, `RemovableTogether`, `Reason`, `FindPair`), `RuleSetAnalysisOutcome` (`Decided`, `Undecided`, `TooManyRules`), `RuleSetPair` (`First`, `Second`, `Overlap` as a `RuleOverlapResult`), `RuleRedundancy` (`Index`, `IsRedundant`, `Example`).
- **Redundancy reading.** Only "any grants" (the set holds when any rule is `True`) is built. The design note's "all required" reading is not built, because nothing asked for it and ticket 07's algorithms do not use it. No reading parameter yet.
- **A pair reuses `RuleOverlapResult`**, so a pair can be `Undecided` on its own. A pair's examples are narrowed to the terms of its two rules, so they look like `RuleOverlap.Compare` output.
- **Term cap or node budget over the set (conservative choice, design note section 5).** `Undecided`, with `Rules` and `RemovableTogether` empty. The pairs are still computed one at a time with `RuleOverlap.Compare`, each with the term cap counted over its pair and its own node budget. `TooManyRules` computes nothing.
- **`maxRules` below 1** throws `ArgumentOutOfRangeException` (programming error, like a `null` rule). A set over the cap does not throw.
- **Redundant rule example.** A rule that is not redundant carries one assignment where it alone is `True`. The covering indexes of a redundant rule (design note sketch) are not reported, because a minimal covering set costs extra BDD work and the ticket did not ask for it.
- **Node budget.** `BddManager.DefaultMaxBddNodes` (100,000) is applied in `Analyzer.FindDifference`, `CompareSatisfaction`, `JointOutcomes` and `AnalyzeSet`. These are the only analyzer methods that `RuleEquivalence`, `RuleImplication`, `RuleOverlap` and `RuleSetAnalysis` call, and each public entry point catches `BddNodeBudgetExceededException` and returns its `Undecided` outcome with the exception message as `Reason`. `RuleDiff`, `RewritePlayground`, `RuleStatistics`, `RuleAssertions` and `RewriteAssertions` call those public entry points, so the exception cannot reach them. Compile-time analysis, `Profile`, `BddNodeCount` and `TargetFunction` (counterfactuals) keep a manager with no budget, as `BddManager`'s parameterless constructor says. The budget is internal and not an option; `RuleSetAnalysis.AnalyzeCore` takes it as a parameter for tests only. `BddNodeBudgetExceededException` now derives from `InvalidOperationException` (Sonar S3871 rejects an internal exception that derives from `Exception` directly).
- **Docs.** New "Rule set analysis" and "The node budget" sections in `docs/rewriting-rules.md`, a row in `docs/architecture.md`, two `CHANGELOG.md` entries. Not done: glossary or `CONTEXT.md` entries for "redundant rule" or "rule set". The page defines them. Add them if the owner wants them as domain terms.
- **Assumptions between terms** are still not built (owner decision: after slice 1, optional argument).
