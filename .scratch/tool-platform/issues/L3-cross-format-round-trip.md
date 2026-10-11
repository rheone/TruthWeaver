# L3: Cross-format round-trip guarantee

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** A property test and a public helper that prove: for generated rules (including notes and every operator, literal kind and data-source variable), converting between rule text, JSON and YAML in all six directions gives a document that compiles to an equal tree (and equal notes). Find and fix any printer or reader gap the test exposes (the text round trip is property-tested today; the other directions are not confirmed). Add public conversion helpers on `CompiledRule` and the compiler if they are missing, with `Try…` forms.

**Blocked by:** L1

**Status:** ready-for-agent

- [ ] All six conversions are covered by one generated-rule property test over many seeds
- [ ] Gaps found are fixed and listed in the ticket
- [ ] `docs/rule-formats.md` states the guarantee and its exceptions (rewrites drop notes)
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
