# 11: Catalog-wide NotX twin invariant test

**What to build:** One test guards the twin rule for the whole catalog. It enumerates every registered predicate, requires a `NotX` twin for each positive one (and no twin for a predicate that is itself a negation), and asserts for True, False and Unknown selected values that `NotX(v)` equals `NOT X(v)`. A deliberately missing twin or a twin that maps Unknown to a definite value makes it fail.

**Blocked by:** 03, 04, 05, 06, 08 (and 07 if its predicates take twins)

**Status:** done

- [x] The test covers every registered predicate and fails when a twin is missing
- [x] The test fails for a twin that does not return Unknown for an Unknown selected value
- [x] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).

## Comments

- 2026-10-04: Blocked on an owner decision. The test is in `tests/TruthWeaver.Tests/PredicateCatalog/` (`NotXTwinChecker`, the reviewed table `NotXTwinTable`, `NotXTwinInvariantTests`). Fixtures prove it fails for a missing twin, a twin that maps Unknown to a definite value, a non-negated twin and a probe gap without a reason. All 59 real pairs are the K3 complement (registered with `NullBehavior.Unknown`). `CheckCoverage_RealCatalog_ReportsNoFailures_Test` fails because five pre-existing positive factories have no twin: `StringPredicates.EqualsIgnoreCase`, `StartsWith`, `EndsWith`, `EqualsConfigurable` and `CollectionPredicates.SetEquals`. Owner choice: add the twins (new public API plus docs) or list them in `NotXTwinTable` as `NoTwin` with a reason. Side finding: `RuleBuilder.Predicate` rejects a value-type array argument (`long[]`, `Guid[]`...) because it matches `IEnumerable<object>`, although its XML docs say `IEnumerable<T>` is supported; the table passes `object[]`.
- 2026-10-04: Done. The five twins (`NotEqualsIgnoreCase`, `NotStartsWith`, `NotEndsWith`, `NotEqualsConfigurable`, `NotSetEquals`) now exist and are `TwinPair` rows in `NotXTwinTable`, so the coverage test passes against the real catalog. Every NotX twin is the strict Strong Kleene complement under `NullBehavior.False` too (owner decision), so the False-versus-True split note is removed. The `RuleBuilder.ValueToNode` typed-array rejection (`long[]`, `decimal[]`, `bool[]`, `Guid[]`, `DateTimeOffset[]`) is left for a separate ticket; the table keeps its `object[]` workaround. The reversed-bounds compile-time diagnostic stays open in tickets 04 and 06.
