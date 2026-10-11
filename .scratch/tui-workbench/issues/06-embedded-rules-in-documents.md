# 06: Rules inside JSON and YAML documents

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** Open a JSON or YAML document that contains a rule among other content. Pick the rule by a path (JSONPath-style) or by browsing the document tree and pressing Enter. Edit it in the workbench. Save by patching only the rule's own span in the original text, so comments, key order and layout elsewhere are untouched (JSON and YAML both expose source positions), or choose a full round-trip write-back with a warning that YAML comments and layout may be lost. Support several rules in one document (switch between them).

**Blocked by:** 01

**Status:** on-hold until tickets 16 and 20 (feature-backlog-2026-10-11) are done; then ready-for-agent

- [ ] Patch-in-place leaves every byte outside the rule's span unchanged (tested on a commented YAML fixture)
- [ ] Round-trip write-back warns before it runs
- [ ] A path that matches nothing or many shows a clear message; the document is never overwritten without confirmation
- [ ] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
