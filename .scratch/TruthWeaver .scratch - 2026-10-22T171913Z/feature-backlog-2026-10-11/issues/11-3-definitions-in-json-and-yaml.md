# 11-3: Definitions in the JSON and YAML tree formats

**Parent:** [11](11-named-sub-rules-and-parameters.md)

**What to build:** The JSON and YAML tree formats accept a `definitions` section with parameters, and compile to the same tree as the equivalent rule text. Update `rule-tree.schema.json` and the tree readers; reuse 11-2's expansion and diagnostics, and map diagnostic spans to the document locations the formats already report.

**Blocked by:** 11-2

**Status:** done

- [ ] JSON and YAML documents with definitions compile to trees equal to the rule-text form (equality tests for several definitions, including nested ones)
- [ ] The schema validates documents with definitions and rejects malformed ones; the schema tests pass
- [ ] Guardrail diagnostics from 11-2 surface through both formats with correct locations
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
