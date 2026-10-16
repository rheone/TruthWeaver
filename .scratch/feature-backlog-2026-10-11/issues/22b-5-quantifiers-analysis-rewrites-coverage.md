# 22b-5: Quantifiers in analysis, rewrites and coverage

**Parent:** [22b](22b-quantifiers-build.md)

**What to build:** The analyzer treats a quantifier as an opaque atom and analyzes its body separately. Rewrites pass it through, with De Morgan in `ToNnf`. Rule coverage folds element children (the exact child-count assertion in the coverage accumulator must cope with a variable-size node).

**Blocked by:** 22b-3

**Status:** ready-for-agent

- [ ] Equivalence and implication checks work on rules containing quantifiers, treating each as an opaque term
- [ ] `ToNnf` negation tests; other rewrites leave the node unchanged
- [ ] Coverage over a corpus works for quantifier rules
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
