# 11-5: Cross-form equality and expansion property test

**Parent:** [11](11-named-sub-rules-and-parameters.md)

**What to build:** The closing tests and documentation sweep for definitions: one cross-form equality test over rule text, JSON, YAML and the builder for the same definitions, a property test that expanding definitions by hand gives a tree equal to the compiler's expansion, and a final pass over every page that describes definitions (rule text, JSON and YAML formats, builder, diagnostics table, glossary).

**Blocked by:** 11-3, 11-4

**Status:** done

- [ ] Cross-form equality test passes for a representative set of definitions
- [ ] Hand-expansion property test passes
- [ ] The parent ticket's acceptance list is fully met, and each page it names is current
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
