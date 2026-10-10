# 02: Public registry schemas and rule predicate names

**What to build:** A host lists the predicates a registry holds and the predicates a compiled rule uses.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `PredicateRegistry<TContext>.Schemas` is public and lists every registered schema
- [x] `CompiledRule<TContext>.PredicateNames` is public and lists each predicate the rule references once, in the casing term identity uses
- [x] Both are documented and tested, including a rule that repeats a predicate
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes. Note 2026-10-10: the feature commit exists and CI is green on the branch head, but a pre-commit hook pass cannot be confirmed from history.

See also [spec](../spec.md).
