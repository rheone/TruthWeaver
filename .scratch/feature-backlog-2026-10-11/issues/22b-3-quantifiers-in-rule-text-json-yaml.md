# 22b-3: Quantifiers in rule text, JSON and YAML

**Parent:** [22b](22b-quantifiers-build.md)

**What to build:** `FORALL(line IN lines, body)` and `EXISTS(...)` in rule text, JSON and YAML (with `rule-tree.schema.json`), compiling to the same tree as the builder form. Update the `OperatorDefinitions` entry, parser, node compiler, `NodeShape` and printers. Check the TextMate grammar (17a) if it has shipped.

**Blocked by:** 22b-2

**Status:** done

- [x] Cross-form equality test (rule text, JSON, YAML, builder) passes
- [x] The schema accepts and rejects the right documents
- [x] Every printer writes the node and round-trips
- [x] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
