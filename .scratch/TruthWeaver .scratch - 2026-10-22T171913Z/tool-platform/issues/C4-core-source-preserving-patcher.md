# C4: Core: same-format patching that keeps comments and layout

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** Edit a rule inside a text, JSON or YAML file and write it back so that only the rule's own span changes: comments, key order, indentation and everything else in the document stay byte for byte. Use source positions from the readers (YamlDotNet exposes marks; JSON positions come from the existing span locator). When a patch cannot be made safely (the rule's structure changed in a way the span cannot hold), fall back to a clearly reported full rewrite of that rule. A full round-trip write-back mode (re-serialize the whole document) exists, off by default, and warns that YAML comments and layout may be lost. Node notes (L1) are patched like other content.

**Blocked by:** C1, L1

**Status:** done

- [x] A commented YAML fixture and a commented JSON-with-notes fixture: bytes outside the rule's span are unchanged after an edit (tested)
- [x] The fallback is reported and tested; the round-trip mode warns
- [x] Several rules in one document patch independently
- [x] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11, dispatch run: implemented as `RulePatcher` in Tool.Core (`Patch` for one `LoadedRule` or a list of `RuleEdit<TContext>`, `WriteBackMode.Patch`/`RoundTrip`, `DocumentPatch`, `RulePatchOutcome`, `RulePatchKind`, `WriteFile`/`Encode`). Choices made without the owner, most conservative option each time:
  - The edit is a `CompiledRule` (what the TUI's linked panes hold), not new source text. Patching compares the tree-format JSON (with notes) of the rule in the document with the edit and replaces changed scalars in place; any shape change (added/removed node, key such as a new `note`, operand) rewrites the whole rule span (`TWT0019` warning), as the ticket says, not the smallest changed subtree.
  - Rule text (a `.rule` file or a string holding rule text) has no per-node positions, so a changed rule text is always rewritten (also `TWT0019`); the string keeps its quote style or block header.
  - YAML block scalars, aliases, anchored or tagged values are never patched in place (fallback). A change to a rule that is itself anchored is the new `TWT0021` error (`WriteBackFailed`, exit code 2), because any write would change the alias too.
  - Every result is read back and compiled; a mismatch falls back, then fails with `TWT0021`. Round-trip mode always warns (`TWT0020`), for JSON too.
  - `JsonSpanLocator` is internal to `TruthWeaver` with no `InternalsVisibleTo` for Tool.Core, so JSON positions come from Tool.Core's own span-keeping reader (`TreeValue`, from C2) instead of adding an `InternalsVisibleTo`.
  - "Commented JSON" fixture: the JSON reader rejects comments, so the JSON fixture carries notes and unusual layout instead.
  - `OutputWriter` normalizes to LF without BOM, which would change bytes outside the rule, so `RulePatcher.WriteFile`/`Encode` write the original encoding, BOM and line endings.
