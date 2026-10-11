# A: Registry-wide default null behavior

**What to build:** Today every predicate factory takes its own `NullBehavior` (`Unknown` or `False`), so a host that wants SQL-style `False` for missing data must pass it to every registration. Add one place to set it: a default null behavior on `PredicateRegistryBuilder` (or the smallest equivalent), applied to every registered predicate that did not get an explicit setting, with `Unknown` as the default and an explicit per-predicate setting always winning. A host can also set it for one family (for example the ordering predicates) through the same mechanism. Work out the smallest design that fits how factories return `(Schema, Evaluate)` before the builder sees them (for example an unset marker on the factory parameter resolved by the builder), and record why in the ticket. `IsNull`/`IsNotNull` have no option and are unaffected. Faults are never affected. Document, on `docs/strong-k3/predicates/README.md` (Null selected values) and `docs/predicate-conventions.md`, when `Unknown` is possible for each predicate group (null value, fault, data-source failures) and the fail-closed reason `Unknown` is the default.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A host sets the default once and every predicate without an explicit setting follows it; an explicit setting wins; the default stays `Unknown`
- [x] A per-family way to set it exists and is tested
- [x] Tests show `NOT (age > 18)` with a null age is `Unknown` by default and `True` when the host sets `False`, so the trade-off is on record
- [x] The "when is Unknown possible" table is in the docs
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

2026-10-11, dispatch run: Design and why. The factories return `(Schema, Evaluate)` with the null behavior already closed into the delegate, and `TruthWeaver` (home of `PredicateRegistryBuilder`) may not reference `TruthWeaver.Predicates` (architecture test), so a builder property cannot reach the factories. Chosen: `NullBehaviorScope` (an `AsyncLocal` stack, `using (NullBehaviorScope.Begin(NullBehavior.False)) { ... }`) plus `PredicateFamily` for the per-family form, and a new unset marker `NullBehavior.Default` (value 2) as the default of every factory's `nullBehavior` parameter. Each public factory resolves the marker when it runs (`NullBehaviorScope.Resolve`), so evaluation is unchanged. Precedence: explicit argument, then family scope, then all-families scope, then `Unknown`. Families map to the factory classes (Numeric, Scalar, String, DateTime, Collection, Regex, Type); "ordering predicates" across numeric and date-time need two scopes. The "when is Unknown possible" table is in `docs/strong-k3/predicates/README.md` and `docs/predicate-conventions.md`.
