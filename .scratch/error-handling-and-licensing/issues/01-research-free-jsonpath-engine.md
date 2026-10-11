# 01: Research a JSON Path engine that is free at every tier

**Type:** research

**What to build:** Decide the JSON Path dependency for `TruthWeaver.DataSources.Json`. The requirement: free to use at every tier, no license, fee or EULA obligation, for binaries as well as source. Then swap it in.

**Blocked by:** None (can start immediately)

**Status:** done

Known so far (verify each; web results are not authoritative):

- `JsonPath.Net` 3.0.0+ uses the Open Source Maintenance Fee EULA for the published binaries (source stays under its OSI license). 2.2.0 is MIT and currently pinned; it has a known `IndexOutOfRangeException` on a query ending in `.` that `JsonPaths.TryParse` works around.
- `Blazing.Json.JSONPath` 1.1.0 (January 2026): MIT, claims RFC 9535 compliance (324 tests), net10.0+, depends on System.Text.Json only. Unconfirmed: `JsonNode` support, trim/AOT compatibility, maintenance activity.
- `JsonCons.JsonPath`: license, RFC 9535 status and net11 compatibility not confirmed.
- Option: build our own RFC 9535 subset. ADR-0006 rejected this as too large a surface; revisit only if no library qualifies.

- [x] Each candidate's license is read from its published package and repository, not from a summary
- [x] Each candidate is checked for RFC 9535 conformance, `net11.0`, trim/AOT warnings (the repo builds with `IsAotCompatible`), dependency licenses and maintenance activity
- [x] The recommendation and rejected alternatives are recorded in ADR-0006 (amended in place and marked)
- [x] The chosen engine replaces `JsonPath.Net` behind `JsonPaths`; the `JsonDataSource` and `JsonQueryValidator` tests pass unchanged in meaning, including the `$.` malformed-query case
- [x] If the package or its transitive dependencies change, `Directory.Packages.props` and the lock files are updated
- [x] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 1).

## Comments

2026-10-04: Done. Recommendation: Meziantou.Framework.JsonPath 3.0.7 (MIT, `net11.0` build, no dependencies, RFC 9535 tested against the compliance suite, reads `JsonNode` directly, active). Rejected, with reasons, in ADR-0006's amendment: Blazing.Json.JSONPath (net10.0 only, `JsonElement` only, no activity after its first two days), JsonCons.JsonPath (not RFC 9535, stale), Hyperbee.Json (Roslyn scripting dependencies), Corvus.Text.Json.JsonPath (second JSON model), JsonPath.Net 3.x (EULA), own subset (too large). Licenses were read from each package's `.nuspec` and `LICENSE` file.

Swap: `JsonPaths` uses `JsonPath.Parse`; the position is read from the "at position N" text of the `FormatException` (no offset property exists), and the `$.` workaround is removed (the package reports it natively). Filter number comparison uses doubles and `match()`/`search()` take I-Regexp only (noted in the ADR). `Directory.Packages.props`, CHANGELOG, `docs/packages.md`, the ADR-0004 note and the architecture test were updated. No lock files are tracked. Build, `dotnet test` (2554), csharpier, `dotnet format` and roslynator pass; the Json and Yaml data source tests are unchanged.
