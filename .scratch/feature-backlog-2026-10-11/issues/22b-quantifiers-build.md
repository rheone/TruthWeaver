# 22b: Quantifiers: build (parent)

**Group:** E logic and analysis

**What to build:** `FORALL` and `EXISTS` over collections. Design note: [22-quantifiers-design.md](../notes/22-quantifiers-design.md) (authoritative; section 11 is the first slice). This file is the parent and holds the owner's decisions (grilled 2026-10-16); the work is in 22b-1 to 22b-6.

Decisions:

- **Slice:** go, typed collection bindings first; data-source ranges later as 22c.
- **Element reads:** typed element predicates over `Element<TOuter, TItem>` and untyped data-source nodes via `from(...)`, both in the first slice.
- **DSL:** `FORALL(line IN lines, body)` and `EXISTS(...)` with a contextual `IN`. The bound variable is required and part of tree equality.
- **Semantics:** an empty collection gives `FORALL` True and `EXISTS` False. A `null` collection is `Unknown` with no fault; a throwing selector is `Unknown` plus a fault. Faults share the whole-evaluation `FaultBudget`. Nesting is allowed (bounded by `MaxDepth` and `MaxQuantifierDepth`).
- **Limits:** `EvaluationOptions.MaxElementEvaluations`, default 10,000; over it gives `Unknown` plus a fault and the evaluation aborts.
- **Trace:** every element recorded in slice 1. Faults inside a body carry an optional additive `Fault.Scope` (for example `lines[734]`).
- **Analysis:** a quantifier is an opaque atom; its body is analyzed on its own. Ticket 21's analysis follows the same rule.
- **Record:** a new ADR amends ADR-0005's operator and connective lists in place, dated.
- **Not in slice 1:** data-source ranges (22c), counted quantifiers, `∀`/`∃` aliases, outer predicates inside a body, trace caps, quantifier folds in `Simplify`.

**Blocked by:** 22 (done) and the grilling (done).

**Status:** split (owner, 2026-10-16) into 22b-1 to 22b-6. Done when all six are done.

- [ ] Scope confirmed with the owner: no new packages, projects or dependencies (stop and ask if one is needed)
- [ ] All sub-tickets are done and the full validation set from `CLAUDE.md` passes
