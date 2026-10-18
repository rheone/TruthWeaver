# 15: Definitions in the tree pane

**What to build:** The tree pane shows each definition and each use of a definition (backlog 11) as a node you can expand to see the body, with the decision marked on each node. Structural edits (ticket 04) work on the expanded rule. Diagnostics inside a definition point to the definition's text. The palette lists the rule's definitions.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] A definition with parameters shows its parameters, and a use shows its arguments
- [ ] Expanding a use shows the body with the arguments substituted
- [ ] A structural edit on an expanded node edits the definition body or the use argument, and refuses an edit that would break the definition
- [ ] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
