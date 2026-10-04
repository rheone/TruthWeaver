# 01: Research a JSON Path engine that is free at every tier

**Type:** research

**What to build:** Decide the JSON Path dependency for `TruthWeaver.DataSources.Json`. The requirement: free to use at every tier, no license, fee or EULA obligation, for binaries as well as source. Then swap it in.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

Known so far (verify each; web results are not authoritative):

- `JsonPath.Net` 3.0.0+ uses the Open Source Maintenance Fee EULA for the published binaries (source stays under its OSI license). 2.2.0 is MIT and currently pinned; it has a known `IndexOutOfRangeException` on a query ending in `.` that `JsonPaths.TryParse` works around.
- `Blazing.Json.JSONPath` 1.1.0 (January 2026): MIT, claims RFC 9535 compliance (324 tests), net10.0+, depends on System.Text.Json only. Unconfirmed: `JsonNode` support, trim/AOT compatibility, maintenance activity.
- `JsonCons.JsonPath`: license, RFC 9535 status and net11 compatibility not confirmed.
- Option: build our own RFC 9535 subset. ADR-0006 rejected this as too large a surface; revisit only if no library qualifies.

- [ ] Each candidate's license is read from its published package and repository, not from a summary
- [ ] Each candidate is checked for RFC 9535 conformance, `net11.0`, trim/AOT warnings (the repo builds with `IsAotCompatible`), dependency licenses and maintenance activity
- [ ] The recommendation and rejected alternatives are recorded in ADR-0006 (amended in place and marked)
- [ ] The chosen engine replaces `JsonPath.Net` behind `JsonPaths`; the `JsonDataSource` and `JsonQueryValidator` tests pass unchanged in meaning, including the `$.` malformed-query case
- [ ] If the package or its transitive dependencies change, `Directory.Packages.props` and the lock files are updated
- [ ] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 1).
