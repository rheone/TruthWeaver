# 06: `IDataSource.ScopeAsync` reports failure through a result, not exceptions

**What to build:** `ScopeAsync` currently throws `InvalidOperationException` (zero or several matches) or `ArgumentException` (malformed query) because the interface has no error channel. Async members cannot use `out`, so return a result type in the style of `DataQueryResult` (success carries the scoped `IDataSource`; failure carries `DataQueryErrorKind` and a message with no resolved data). Update `JsonDataSource`, `YamlDataSource`, `FakeDataSource` and every consumer.

**Blocked by:** 01

**Status:** ready-for-agent

This is a breaking change to a public interface. Nothing has been released and the only consumers are in this repository, so record it in the CHANGELOG and amend ADR-0006 in place.

- [ ] Tests first: zero matches, several matches, malformed query and an unsupported type each produce a failure result and never throw
- [ ] A successful scope still roots at a copy of the matched node
- [ ] `FakeDataSource.WithScope` and its failure scripting follow the new shape
- [ ] ADR-0006, `docs/data-sources.md` and the CHANGELOG are updated
- [ ] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (Try-candidate survey).
