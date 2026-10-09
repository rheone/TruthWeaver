# 02: Public registry schemas and rule predicate names

**What to build:** A host lists the predicates a registry holds and the predicates a compiled rule uses.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `PredicateRegistry<TContext>.Schemas` is public and lists every registered schema
- [ ] `CompiledRule<TContext>.PredicateNames` is public and lists each predicate the rule references once, in the casing term identity uses
- [ ] Both are documented and tested, including a rule that repeats a predicate
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
