# 01: Refresh the library roadmap and deferred features

**What to build:** Re-score .scratch/library-roadmap/spec.md and .scratch/deferred-features/spec.md now that the Strong K3 language surface, rewrites (expand, compress, canonicalise, simplify), structured diagnostics and the dual-rail analyzer exist. Several verdicts are stale: the BDD-based equivalence check and 'simplify my rule' overlap Canonicalize and Simplify; ready-made predicate factories must return TruthValue; the fuzzer can reuse the shared rule generator; De Morgan / negation-normal-form printing overlaps the rewrites. Update each item's impact, complexity and verdict, add new candidates raised by the research and audit (see the k3-hardening tickets), and remove or mark obsolete items.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every item in both documents has a current verdict and rationale or is marked obsolete with a reason
- [x] New items from k3-hardening and k3-followups are cross-referenced
- [x] No code is changed

See also [spec](../spec.md).

## Comments

- Documentation only; no code changed. Verdicts were re-checked against `src/` and `git log`, not the old text. `.scratch/library-roadmap/spec.md` gained a "Re-score (2026-10-03)" section (every original item re-scored with a rationale, plus a "New candidates" table and open owner decisions); the original per-item prose is kept and marked superseded where it disagrees. `.scratch/deferred-features/spec.md` gained a matching per-row re-score table.
- Outcomes: rule-equivalence is **Done** (`RuleEquivalence`, `PreservesMeaning`); K3 lint rules are **Done** (residual style lints stay open); the culture/case `EqualsConfigurable` extension is **Obsolete** (culture removed); BDD "simplify my rule" is **Obsolete** as a goal (`Simplify()` exists), with the BDD form Not now; De Morgan/NNF print mode is Not now (overlaps `Simplify`/`Canonicalize`); predicate factories must return `TruthValue`; the fuzzer is re-shaped (the generator is `K3RuleGenerator` and `K3Oracle` in test support, not yet in `TruthWeaver.Testing`). OpenTelemetry is partly delivered (a `Meter` exists, no `ActivitySource`); minimal satisfying assignments are partly delivered (internal, feeds counter-examples).
- New candidates cross-referenced from k3-hardening 04, 05, 07, 08, 09, 10 and k3-followups 26, 28, 31, 33.
