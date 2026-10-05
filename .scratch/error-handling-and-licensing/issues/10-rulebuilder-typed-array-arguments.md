# 10: `RuleBuilder` accepts typed array arguments

**What to build:** `RuleBuilder.ValueToNode` (`src/TruthWeaver/Building/RuleBuilder.cs`) matches an array argument as `IEnumerable<object>`. A value-type array does not match, because array covariance does not apply to value types. `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` arguments therefore throw `ArgumentException`, although the XML docs of `RuleBuilder.Predicate` promise `IEnumerable<T>`. Make every typed array work, with the same node shape the `object[]` form builds today. Then remove the `object[]` workaround and its note in `tests/TruthWeaver.Tests/PredicateCatalog/NotXTwinTable.cs`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Tests first, one for each element kind: `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` each build the same tree as the equivalent `object[]`, and each fails before the change
- [x] `string[]`, `object[]` and a `List<T>` of each kind still work, and a mixed-kind or unsupported element still throws `ArgumentException`
- [x] The XML docs of `RuleBuilder.Predicate` match the behavior
- [x] The `object[]` workaround and its note are removed from `NotXTwinTable`, and `NotXTwinInvariantTests` still pass
- [x] CHANGELOG lists the fix
- [x] The full validation from CLAUDE.md passes

Source: [predicate-catalog ticket 11](../../predicate-catalog/issues/11-notx-twin-invariant-test.md) (side finding).

## Comments

Done. `RuleBuilder.ValueToNode` now matches the non-generic `System.Collections.IEnumerable` (strings match earlier), so value-type arrays and any `List<T>` build the same node as `object[]`; each element still goes through `ValueToNode`, so an unsupported element type throws `ArgumentException`. Mixed-kind arrays were never rejected by the builder (the rule compiler rejects them) and that is unchanged. New tests: `tests/TruthWeaver.Tests/RuleBuilderTypedArrayTests.cs` (red before the fix for `long[]`, `decimal[]`, `bool[]`, `Guid[]`, `DateTimeOffset[]` and the lists). The `object[]` workaround and note are gone from `NotXTwinTable` (static `long[]`/`decimal[]`/`bool[]` fields, CA1861); `NotXTwinInvariantTests` pass. CHANGELOG `Fixed` entry added.
