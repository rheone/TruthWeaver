# Joining compiled rules

**Status:** ready-for-agent

Source: [library-roadmap](../library-roadmap/spec.md) re-score (2026-10-03), grilled 2026-10-09.

## Problem Statement

Only one tree is built from scratch at a time. To combine two rules that are already compiled (`ruleA AND ruleB`), a host
must print one to JSON or text and rebuild it by hand. This blocks any policy-combination use, such as joining a base
rule with a tenant rule.

## Solution

`RuleBuilder.FromCompiled(CompiledRule<TContext>)` returns a builder that holds the compiled rule's tree. A host joins
rules with the builder operators that already exist, for example
`RuleBuilder.And(RuleBuilder.FromCompiled(a), RuleBuilder.FromCompiled(b))`, then compiles the result.

## Decisions

- **Soundness.** The two source rules may come from different registries, so the join is not trusted. The joined builder
  compiles against the destination registry, which validates every term. A predicate missing from the destination, or
  with a different schema, gives the normal compile diagnostics. No separate validation code exists.
- **Surface.** One new member, `FromCompiled`. There are no `And` or `Or` shortcuts on `CompiledRule` and no policy
  combinators. Every present and future operator works through the builder.
- **Other compile rules apply unchanged.** Data-source declarations (`TRE0024`), argument checks and `CompilerOptions`
  limits (depth and size) apply to the joined tree as to any builder rule.
- **Same context type.** Both rules share `TContext`. Rules for different context types cannot be joined.
- **Term identity.** A term in both source rules keeps one identity in the joined tree, so it is evaluated once.

## Out of scope

- Policy-combination algorithms (permit-overrides, deny-overrides). They belong to the deferred authorization layer.
- Shortcut methods on `CompiledRule`.
- Joining rules of different `TContext` types.

## Further notes

- Carries XML docs, tests named per CLAUDE.md, and the full validation set. Tests cover a join across two registries
  where the destination lacks a predicate, a shared term evaluated once, and a joined tree above the depth limit.
- The README and builder guide document the join. `FromCompiled` adds no operator, so the K3 reference sync does not
  apply.
