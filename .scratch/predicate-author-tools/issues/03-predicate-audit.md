# 03: PredicateAudit.FindUnused

**What to build:** A host finds the registered predicates that none of its stored rules use.

**Blocked by:** 02 (Public registry schemas and rule predicate names)

**Status:** done

- [x] `PredicateAudit.FindUnused(registry, rules)` returns the registered schemas no rule references
- [x] Name matching uses the normalized casing of term identity
- [x] An empty rule set returns every schema; an empty registry returns none
- [x] It lives in the `TruthWeaver` package and compiles no rule text
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes. Note 2026-10-10: the feature commit exists and CI is green on the branch head, but a pre-commit hook pass cannot be confirmed from history.

See also [spec](../spec.md).
