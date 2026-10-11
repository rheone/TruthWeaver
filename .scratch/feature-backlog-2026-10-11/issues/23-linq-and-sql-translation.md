# 23: LINQ and SQL translation of rules

**Group:** E logic and analysis

**What to build:** Compile a rule to `Expression<Func<T,bool>>` or a SQL `WHERE`, so a database filters rows instead of loading and evaluating each one. SQL's NULL logic is Strong Kleene for AND, OR and NOT and a WHERE drops `Unknown` rows, which matches the fail-closed `IsSatisfied`, so the semantics fit.

Notes recorded 2026-10-11:
- Obstacle: predicates are opaque C# delegates and classes, possibly async or calling services, so the engine cannot see inside them. Only a translatable sub-catalog can work: the built-in comparisons over expression selectors such as `p => p.Age`. The rest stays as an in-memory filter over the narrowed rows (partial translation).
- AOT conflict: `Expression<Func<...>>.Compile()` is not AOT-safe, and this repository keeps the core and `TruthWeaver.Predicates` AOT- and trim-clean (`IsAotCompatible`). Recommended design: a separate package (for example `TruthWeaver.Linq`) with expression-based factories, not a change to the existing catalog. That package is not AOT-promised and says so.
- Alternative designs: change the catalog to take expression selectors (breaks AOT cleanliness and callers passing a `Func` variable), or let the host write a translator per predicate (no catalog change, more host work).
- Usage sketch: `var allowed = await db.Customers.Where(rule.ToExpression()).ToListAsync();` which becomes `WHERE age >= 18 AND license_number IS NOT NULL`. Useful for large tables and list endpoints; not useful for single-resource authorization.
- Needs a new package and an EF Core (or `System.Linq.Expressions`) dependency: owner confirmation first.
- Related: partial evaluation by substitution in `deferred-features`.

**Blocked by:** None (can start immediately)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
