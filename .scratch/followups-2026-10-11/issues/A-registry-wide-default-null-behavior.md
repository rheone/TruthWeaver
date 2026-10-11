# A: Registry-wide default null behavior

**What to build:** Today every predicate factory takes its own `NullBehavior` (`Unknown` or `False`), so a host that wants SQL-style `False` for missing data must pass it to every registration. Add one place to set it: a default null behavior on `PredicateRegistryBuilder` (or the smallest equivalent), applied to every registered predicate that did not get an explicit setting, with `Unknown` as the default and an explicit per-predicate setting always winning. A host can also set it for one family (for example the ordering predicates) through the same mechanism. Work out the smallest design that fits how factories return `(Schema, Evaluate)` before the builder sees them (for example an unset marker on the factory parameter resolved by the builder), and record why in the ticket. `IsNull`/`IsNotNull` have no option and are unaffected. Faults are never affected. Document, on `docs/strong-k3/predicates/README.md` (Null selected values) and `docs/predicate-conventions.md`, when `Unknown` is possible for each predicate group (null value, fault, data-source failures) and the fail-closed reason `Unknown` is the default.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A host sets the default once and every predicate without an explicit setting follows it; an explicit setting wins; the default stays `Unknown`
- [ ] A per-family way to set it exists and is tested
- [ ] Tests show `NOT (age > 18)` with a null age is `Unknown` by default and `True` when the host sets `False`, so the trade-off is on record
- [ ] The "when is Unknown possible" table is in the docs
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
