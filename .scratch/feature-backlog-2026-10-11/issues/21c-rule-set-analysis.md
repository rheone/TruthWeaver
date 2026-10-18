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
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
