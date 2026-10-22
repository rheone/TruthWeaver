# 04: JSON data source package

**What to build:** A real JSON document works as a data source: the new `TruthWeaver.DataSources.Json` package provides `JsonDataSource` over JsonPath.Net (RFC 9535), node-to-`LiteralValue` conversion, and `ScopeAsync` to root a source at a subtree (decisions 5, 8 and 14). Before building it, confirm JsonPath.Net's license, maintenance state and .NET 11 and AOT-analyzer compatibility, and report back if it fails any, since the alternatives are in ADR-0006.

**Blocked by:** 01

**Status:** done

- [x] `$.orders[?@.id=='A7'].total` returns the one matching order's total, and several matches report as ambiguous
- [x] `ScopeAsync` roots a source at one repeated subtree and queries stay absolute within it
- [x] The core `TruthWeaver` package gains no third-party dependency; Architecture tests updated for the new package
- [x] The AOT and trim analyzers stay clean
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- **License and compatibility check (JsonPath.Net).** 3.0.0 and later (3.0.2 is current) ship under the Open Source Maintenance Fee EULA (`OSMFEULA.txt`), not MIT; the ADR's open license question therefore has a real answer. The package is pinned to **2.2.0** (MIT, RFC 9535, targets net8.0/net9.0/net10.0/netstandard2.0, depends only on Json.More.Net 2.2.0, also MIT), which restores on net11.0 and builds clean under the trim and AOT analyzers. The pin and its reason are in `Directory.Packages.props`. Owner decision needed: stay on 2.2.0 (no updates), accept the OSMF terms for 3.x, or pick another engine (ADR-0006 lists the alternatives).
- **Public API.** `TruthWeaver.DataSources.Json.JsonDataSource : IDataSource` with `Parse(string json)` (throws `JsonException` for malformed JSON) and `Create(JsonNode? root)`. `ScopeAsync` returns a source over a deep copy of the one matched node and throws `InvalidOperationException` (none or several matches) or `ArgumentException` (malformed query), because the interface has no error channel and scoping is host code.
- Several matches are returned in document order; the engine reports ambiguity. Zero matches is a successful empty result.
- Tests: `JsonDataSourceTests` in the new `TruthWeaver.DataSources.Json.Tests` project, boundary tests in `TruthWeaver.Architecture.Tests` (core and abstractions reference no JSONPath assembly; the JSON package depends on abstractions alone).
