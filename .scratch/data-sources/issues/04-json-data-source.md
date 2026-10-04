# 04: JSON data source package

**What to build:** A real JSON document works as a data source: the new `TruthWeaver.DataSources.Json` package provides `JsonDataSource` over JsonPath.Net (RFC 9535), node-to-`LiteralValue` conversion, and `ScopeAsync` to root a source at a subtree (decisions 5, 8 and 14). Before building it, confirm JsonPath.Net's license, maintenance state and .NET 11 and AOT-analyzer compatibility, and report back if it fails any, since the alternatives are in ADR-0006.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `$.orders[?@.id=='A7'].total` returns the one matching order's total, and several matches report as ambiguous
- [ ] `ScopeAsync` roots a source at one repeated subtree and queries stay absolute within it
- [ ] The core `TruthWeaver` package gains no third-party dependency; Architecture tests updated for the new package
- [ ] The AOT and trim analyzers stay clean
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
