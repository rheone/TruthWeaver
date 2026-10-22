# 33: Make the `RuleBuilder` params vs `IEnumerable` overload difference hard to trip over

**What to build:** `RuleBuilder.And(array)` binds the `params` overload, which the compiler rejects for 0 or 1 items, while `And(list)` binds the `IEnumerable` overload, which folds to a constant or the single operand. The difference is documented (followup 17) but easy to trip over. Decide whether better XML docs and a README note suffice, or whether an analyzer or API change is worth it. Do not change the fold behaviour; it was a deliberate decision.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The owner picks: documentation only, or an API or analyzer change
- [ ] The chosen change is made, with the array-versus-list case shown in the XML docs

Source: review of PR #4, Spec axis, questionable item on `RuleBuilder`. Related: [22](22-counted-operator-enumerable-overloads.md).

## Comments

- 2026-10-03: Owner chose documentation only. Work tracked by k3-followups 37.
- 2026-10-03: Resolved by k3-followups 37 (XML docs and README note added).
