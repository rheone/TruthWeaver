# Rewrite tooling: soundness checks, normal forms, evaluation order and provenance

**Status:** grilled 2026-10-10, ready for implementation

Source: [library-roadmap](../library-roadmap/spec.md) ("Not now" NNF print mode), the simplification discussion of 2026-10-10,
and [deferred-features](../deferred-features/spec.md).

## Problem statement

A rule has three kinds of rewrite today: `Canonicalize()`, `Simplify()` and the primitive expansions. All are K3-sound and none
changes the result. Four gaps remain:

- A consumer who writes a rewrite, or who wants to check one, has no public soundness check. `RuleFuzzer` checks `Simplify` and
  `Canonicalize` on generated rules only. `AssertEquivalent` checks one pair.
- There is no way to get a rule in negation normal form (NNF), conjunctive normal form (CNF) or disjunctive normal form (DNF).
  `Simplify()` applies De Morgan only where it removes nodes.
- No rewrite targets evaluation cost. `Canonicalize()` orders operands by text, and its documentation says a canonical rule is "not
  for performance tuning". Evaluation is left to right with short-circuit, so operand order decides which predicates run.
- `Simplify()` changes a rule and does not say how. An author cannot see which law fired.

## Solution

Four tickets.

| Ticket | Change | Status |
| --- | --- | --- |
| [01](issues/01-rewrite-soundness-assertion.md) | `RewriteAssertions.AssertSound`: equivalence, never-larger and idempotence, on any rewrite. | ready |
| [02](issues/02-normal-forms.md) | `ToNnf()`, `ToCnf()` and `ToDnf()` on `CompiledRule`. | ready |
| [03](issues/03-evaluation-order-optimization.md) | A cost hint on `PredicateSchema` and an operand reorder for `AND` and `OR`. | benchmark first |
| [04](issues/04-rewrite-provenance.md) | A report of the laws a rewrite applied. | ready |

Ticket 01 goes first. Tickets 02 and 03 use it as their safety net.

## Decisions

- **K3 soundness is the only bar.** A rewrite must give the same Strong Kleene value for every `True`/`False`/`Unknown` assignment.
  No classical law is used where it fails for `Unknown`: `a OR NOT a` is not `True`, and complement absorption is rejected.
- **Distribution holds in K3.** The K3 connectives form a distributive lattice with De Morgan negation. `AND` distributes over
  `OR` and the reverse, so CNF and DNF are sound.
- **Opaque operators are leaves.** `COALESCE`, `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` and `If` are not information-monotone.
  A normal form treats each as an atom and does not push `NOT` into it.
- **Size cap.** A rewrite that can grow a rule (CNF, DNF, and NNF over expanded derived operators) honors
  `CompilerOptions.MaxRewriteNodeCount` and reports `TRE0016` (`RewriteTooLarge`), the same as the existing rewrites.
- **Value, not order.** A rewrite that reorders may change which predicates run and which faults appear. The value never changes.
  This matches the existing documentation of `Simplify()`.

## Resolved questions (grilled 2026-10-10)

1. **NNF over thresholds.** Thresholds (`AtLeast`, `AtMost`, `Exactly`, `BETWEEN` and the rest of the family) expand to
   `AND`/`OR`/`NOT`. Expansion can grow a rule a lot, so it is opt-in. `ToNnf`, `ToCnf` and `ToDnf` take an options value with
   `ExpandThresholds` (default off). When it is off and a threshold blocks a strict form, the result keeps the threshold as an
   atom and carries a warning with the growth estimate. When it is on, the rule expands up to `MaxRewriteNodeCount`, and
   `TRE0016` beyond that.
2. **Cost source.** Both. `PredicateSchema.Cost` is a static hint and the default. An opt-in override takes measured values.
   Same inputs and same registry give the same order unless the override is used.
3. **Gate on ticket 03.** Benchmark first. If the gain on realistic mixed-cost rules is small, close the ticket.

## Out of scope

- Guaranteed-minimal forms (Quine–McCluskey). They are NP-hard and two-valued. See the roadmap "Don't do" list.
- BDD-based re-synthesis. Revisit only when a rule defeats `Simplify()`.
- A stable equivalence key for storage. It needs a design spike on the dual-rail BDD.
- Common-subexpression sharing. Memoization by term identity covers the common case.
- A public tree edit API. It belongs with substitution (`Bind` plus `Simplify()`).

## Further notes

- Every ticket carries XML docs, tests named per CLAUDE.md with a `<summary>`, and the full validation set from CLAUDE.md.
- A new public rewrite on `CompiledRule` extends `RuleFuzzCheck`, so the fuzzer checks it against the K3 oracle.
- No predicate or operation is added, so the K3 reference sync does not apply. If a rewrite is user-visible, confirm with the
  owner where its documentation page goes.
