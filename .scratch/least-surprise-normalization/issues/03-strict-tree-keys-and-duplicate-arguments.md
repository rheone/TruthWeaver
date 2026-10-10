# 03: Reject unknown tree keys and duplicate arguments

**What to build:** A JSON or YAML node with an unknown key, or with conflicting keys (`predicate` and `op` together), is rejected with TRE0014, matching the published schema. A duplicate argument name is rejected with its own diagnostic on every surface (DSL, JSON, YAML, builder), not last-wins.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Failing tests first for: unknown key, `predicate` plus `op`, a stray `k` or `min` on `AND`, a misspelt `arg`, a duplicate argument on each surface
- [ ] The new duplicate-argument code follows the last `DiagnosticCodes` entry and is documented in the diagnostics reference
- [ ] Behavior agrees with `rule-tree.schema.json` (`additionalProperties: false`)
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
