# 04: Structural tree editor, palette and clipboard

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** Select a tree node and apply: replace operator, wrap in `NOT`, negate, insert an operand, delete, swap operands, replace a term from the palette, change a literal. A palette lists operators and constants, predicates from the schema file, from the open document and from the `TruthWeaver.Predicates` catalog as templates (name, arguments and kinds, with a form to fill in values). Every edit is validated by the compiler, updates the text pane and the decision, and is undoable. Clipboard: copy a node or the whole rule as valid expression text, paste valid expression text onto a node.

**Blocked by:** 01, 02

**Status:** done (the TruthWeaver.Predicates catalog templates in the palette are deferred; see Comments)

- [x] Each edit operation has a test that the resulting tree equals the expected tree and compiles
- [x] An edit that would make an invalid rule is refused with a message, not applied
- [x] Undo and redo restore text and tree exactly
- [x] Paste of invalid text is rejected with the diagnostics
- [x] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-17: the palette does not yet offer the `TruthWeaver.Predicates` catalog as templates. That needs a new reference from `TruthWeaver.Tool.Tui` to `TruthWeaver.Predicates`, which needs owner approval. `docs/tui.md` says so.
- 2026-10-17: the owner approved the reference. The work is ticket [10](10-palette-predicates-catalog.md).
