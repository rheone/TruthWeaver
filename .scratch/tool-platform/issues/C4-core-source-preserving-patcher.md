# C4: Core: same-format patching that keeps comments and layout

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** Edit a rule inside a text, JSON or YAML file and write it back so that only the rule's own span changes: comments, key order, indentation and everything else in the document stay byte for byte. Use source positions from the readers (YamlDotNet exposes marks; JSON positions come from the existing span locator). When a patch cannot be made safely (the rule's structure changed in a way the span cannot hold), fall back to a clearly reported full rewrite of that rule. A full round-trip write-back mode (re-serialize the whole document) exists, off by default, and warns that YAML comments and layout may be lost. Node notes (L1) are patched like other content.

**Blocked by:** C1, L1

**Status:** ready-for-agent

- [ ] A commented YAML fixture and a commented JSON-with-notes fixture: bytes outside the rule's span are unchanged after an edit (tested)
- [ ] The fallback is reported and tested; the round-trip mode warns
- [ ] Several rules in one document patch independently
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
