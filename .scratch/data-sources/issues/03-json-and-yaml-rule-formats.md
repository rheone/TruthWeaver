# 03: JSON and YAML rule formats

**What to build:** A variable reference round-trips losslessly between the DSL, JSON (`{ "from": "user", "query": "$.minAge" }`) and YAML, and `rule-tree.schema.json` accepts the new argument shape (decision 10). `from` is reserved as an argument value form.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Round-trip DSL, JSON and YAML to the same canonical text, including queries containing quotes and brackets
- [ ] The JSON Schema validates the new shape and still rejects malformed references
- [ ] Full validation set from CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0006](../../../docs/adr/0006-data-sources-for-expression-variables.md).
