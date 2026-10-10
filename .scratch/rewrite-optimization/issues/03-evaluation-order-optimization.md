# 03: Evaluation-order optimization

**What to build:** A rule author lets the engine run cheap predicates first, so a short-circuit skips the expensive ones.

**Blocked by:** [01](01-rewrite-soundness-assertion.md), and a benchmark result (first checkbox)

**Status:** needs measurement first

- [ ] A benchmark shows that operand order changes evaluation time on a realistic rule with mixed-cost predicates; if it does not, close this ticket
- [ ] `PredicateSchema` gains an optional relative `Cost` set with `init`; unset means neutral, so existing registrations do not change
- [ ] `CompiledRule.OptimizeOrder()` stable-sorts the operands of `AND` and `OR` by subtree cost, cheapest first; `COALESCE`, `IMPLIES` and `If` keep their order
- [ ] A term shared by two operands counts once
- [ ] The result is K3-equivalent and the same size; verified with `AssertSound`
- [ ] The documentation says which predicates may stop running and that faults can differ, as it does for `Simplify()`
- [ ] An opt-in override takes measured per-predicate cost values; without it the order depends only on the static hints
- [ ] The default path gives the same order for the same rule and registry on every run
- [ ] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
