# 01: RuleBuilder.FromCompiled

**What to build:** A host combines two compiled rules under any operator without a JSON round trip.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `RuleBuilder.FromCompiled(CompiledRule<TContext>)` returns a builder holding the rule's tree, usable with every builder operator
- [x] The joined builder compiles against the destination registry; a predicate missing there, or with a different schema, gives the normal compile diagnostics
- [x] Data-source declarations (`TRE0024`) and `CompilerOptions` limits apply to the joined tree
- [x] A term present in both source rules keeps one identity and is evaluated once
- [x] The README or builder guide documents the join with a tested example
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
