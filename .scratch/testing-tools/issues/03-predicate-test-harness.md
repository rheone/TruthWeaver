# 03: Predicate test harness

**What to build:** A predicate author runs standard checks against a real predicate and gets a report of what held.

**Blocked by:** 01 (Testing references TruthWeaver)

**Status:** ready-for-agent

- [ ] `RunAsync` takes a predicate's schema, evaluation delegate and a context, and returns a `PredicateHarnessReport` with one outcome per check
- [ ] Determinism: two calls with the same arguments and context give the same answer
- [ ] Boundary values are generated per declared `LiteralKind` (empty and long strings, `Int64` and `Decimal` limits, `Guid.Empty`, `DateTimeOffset` limits, empty arrays)
- [ ] A thrown exception is a finding unless the author allow-lists it per argument or per exception type; an allow-listed one reports as an expected fault
- [ ] Schema conformance: a declared argument that is never read, and an undeclared one that is read, are reported clearly
- [ ] Cancellation is observed and reported, never enforced
- [ ] An `Unknown` result is valid, and the report says whether it came with a fault
- [ ] `report.ShouldPass()` throws a harness exception that lists the failures
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
