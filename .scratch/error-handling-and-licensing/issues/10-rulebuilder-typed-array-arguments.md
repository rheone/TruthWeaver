# 10: `RuleBuilder` accepts typed array arguments

**What to build:** `RuleBuilder.ValueToNode` (`src/TruthWeaver/Building/RuleBuilder.cs`) matches an array argument as `IEnumerable<object>`. A value-type array does not match, because array covariance does not apply to value types. `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` arguments therefore throw `ArgumentException`, although the XML docs of `RuleBuilder.Predicate` promise `IEnumerable<T>`. Make every typed array work, with the same node shape the `object[]` form builds today. Then remove the `object[]` workaround and its note in `tests/TruthWeaver.Tests/PredicateCatalog/NotXTwinTable.cs`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Tests first, one for each element kind: `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` each build the same tree as the equivalent `object[]`, and each fails before the change
- [ ] `string[]`, `object[]` and a `List<T>` of each kind still work, and a mixed-kind or unsupported element still throws `ArgumentException`
- [ ] The XML docs of `RuleBuilder.Predicate` match the behavior
- [ ] The `object[]` workaround and its note are removed from `NotXTwinTable`, and `NotXTwinInvariantTests` still pass
- [ ] CHANGELOG lists the fix
- [ ] The full validation from CLAUDE.md passes

Source: [predicate-catalog ticket 11](../../predicate-catalog/issues/11-notx-twin-invariant-test.md) (side finding).

## Comments
