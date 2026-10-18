# 15: `CollectionPredicates.IsNullOrEmpty` and `IsNotNullOrEmpty`

**Parent:** [05](05-unknown-consistency-audit.md) D10 (owner decision 2026-10-17)

**Blocked by:** None

**Status:** done

**What to build:** Add the definite null test for collections: `IsNullOrEmpty` and its twin, matching the string `IsNullOrEmpty` (a null collection is `True`/`False` for them, never `Unknown`, whatever `NullBehavior` says). Adds public API in `TruthWeaver.Predicates`. Follow the predicate-catalog conventions (labels, descriptions, options, registry entries) and add the `docs/strong-k3/` pages, category index and root navigation.

- [x] Tests for null, empty, non-empty and a faulting selector
- [x] `docs/strong-k3/` pages added and `dotnet test tests/TruthWeaver.Tests --filter-class "*K3Reference*"` passes
- [x] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
