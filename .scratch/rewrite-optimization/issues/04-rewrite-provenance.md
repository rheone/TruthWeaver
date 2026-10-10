# 04: Rewrite provenance

**What to build:** An author sees which laws `Simplify()` applied, in order, with the before and after text of each step.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A new member returns the simplified rule and a list of steps; `Simplify()` keeps its signature and result
- [x] Each step names the law (constant fold, identity, annihilator, idempotence, double negation, flatten, absorption, De Morgan and so on) and holds the before and after rule text of the changed subtree
- [x] An already-simple rule returns an empty list
- [x] Steps are in the order the rewrite applied them, and the rule in the result equals the rule from `Simplify()`
- [x] A diff renderer can use the step list; document how it relates to `RuleDiff`
- [x] Documentation updated in the same change: `docs/rewriting-rules.md` documents the step list and its relation to `RuleDiff`
- [x] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
