# 06: YAML data source

**What to build:** A YAML document works as a data source with the same queries and validation as JSON. `YamlDataSource` in `TruthWeaver.Yaml` reads YAML into the JSON data model and reuses the JSON package's query engine and validator (decision 14).

**Blocked by:** 04, 05

**Status:** done

- [x] The same query gives the same result against equivalent JSON and YAML documents
- [x] `TruthWeaver.Yaml` references the JSON data source package and nothing else new
- [x] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).

## Resolution notes

- **Public API.** `TruthWeaver.Yaml.YamlDataSource : IDataSource` with `Parse(string yaml)` and `Create(YamlNode root)`; both throw `YamlException` for malformed YAML, duplicate mapping keys or an alias that refers to its own ancestor. It wraps a `JsonDataSource`, so `QueryAsync`, `ScopeAsync` (which returns the JSON source's scoped source) and the validator are shared; there is no separate YAML validator.
- **Scalar rules** (documented on the class): quoted, block and `!!str` scalars are strings; plain `true`/`false` (any case) are booleans; a plain scalar matching JSON's number grammar is a number (the written text is kept, so `18.0` is a decimal); plain `null`, `~` and an empty value are null; everything else, including `yes`, `007` and `2026-10-03`, is a string. Merge keys (`<<`) are not expanded.
- **Package reference.** `TruthWeaver.Yaml` now has a `ProjectReference` to `TruthWeaver.DataSources.Json` and nothing else new; the Architecture tests assert it reaches the JSONPath library only through that package. Tests: `YamlDataSourceTests` in `TruthWeaver.Yaml.Tests`.
