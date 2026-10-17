# 14: Accept the `T`, `F`, `U` constant letters in JSON and YAML rules

**Parent:** [05](05-unknown-consistency-audit.md) D9 (owner decision 2026-10-17; the audit recommended keeping them rejected)

**Blocked by:** None

**Status:** ready-for-agent

**What to build:** The DSL, the rule-test file and printer overrides accept the letters `T`, `F`, `U` for constants, but `{"const": "U"}` in JSON or YAML is a diagnostic. Accept the letters (case rule as in the DSL) in the JSON and YAML tree formats, keep `true`/`false`/`unknown` working, update `rule-tree.schema.json`, the tree-format reader/emitter decision (the emitter keeps its current spelling unless the ticket's tests show a reason), and `docs/rule-formats.md`, which currently says the letters are not in the tree. A round-trip and cross-form equality test must still hold.

- [ ] JSON and YAML rules with each letter compile to the same tree as the DSL
- [ ] `rule-tree.schema.json` accepts the letters and a schema test covers it
- [ ] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
