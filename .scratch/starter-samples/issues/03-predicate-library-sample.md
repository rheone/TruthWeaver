# 03: PredicateLibrary sample

**What to build:** A predicate-only team copies a project that references `TruthWeaver.Abstractions` alone.

**Blocked by:** 01 (Samples scaffolding)

**Status:** done

- [x] `samples/PredicateLibrary` references only `TruthWeaver.Abstractions` and defines predicates with schemas
- [x] One test evaluates a predicate through its schema and delegate
- [x] The architecture rules still pass
- [x] Getting started links to it
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes. Note 2026-10-10: the feature commit exists and CI is green on the branch head, but a pre-commit hook pass cannot be confirmed from history.

See also [spec](../spec.md).
