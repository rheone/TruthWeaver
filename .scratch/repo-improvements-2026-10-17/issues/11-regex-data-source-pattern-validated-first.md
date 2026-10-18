# 11: Validate regex pattern and flags before reading the selection

**Parent:** [05](05-unknown-consistency-audit.md) D2 (owner decision 2026-10-17)

**Blocked by:** None

**Status:** done

**What to build:** `RegexPredicates.Create` returns for a null selection before it reads `pattern` and `flags`, so a bad data-source pattern or flags value is silent until the first non-null value. Read and validate both first (as `ScalarPredicateCore.Range` and the calendar predicates do), so a bad pattern is a Fault even for a null selection. Cover `IsMatch` and `IsNotMatch`.

- [x] A test with a bad `pattern` and a bad `flags` from `from(...)` and a null selected string gives `Unknown` plus a Fault
- [x] A good pattern with a null selection still behaves as before
- [x] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
