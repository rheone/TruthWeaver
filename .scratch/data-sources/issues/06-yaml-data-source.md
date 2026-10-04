# 06: YAML data source

**What to build:** A YAML document works as a data source with the same queries and validation as JSON. `YamlDataSource` in `TruthWeaver.Yaml` reads YAML into the JSON data model and reuses the JSON package's query engine and validator (decision 14).

**Blocked by:** 04, 05

**Status:** ready-for-agent

- [ ] The same query gives the same result against equivalent JSON and YAML documents
- [ ] `TruthWeaver.Yaml` references the JSON data source package and nothing else new
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
