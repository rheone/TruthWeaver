# 02: Collection selector is materialized once per binding and outer element

**Parent:** [main review](../spec.md)

**Blocked by:** 01 (both change CollectionBinding.cs, so they run one after the other)

**Status:** done

**What to build:** Call a collection selector at most once per (binding, outer element) per evaluation and reuse the materialized list (ADR-0010 decision 5).

**Evidence:** CollectionBinding.cs:83 calls selector(context)?.ToList() on every quantifier run; EvaluationState has no selector cache. Two quantifiers over one collection call the selector twice, so a selector that answers differently breaks the predicate-author contract.

- [x] A counting selector is called once when two quantifiers read the same collection in one evaluation
- [x] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
