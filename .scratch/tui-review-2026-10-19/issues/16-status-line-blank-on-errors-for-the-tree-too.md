# 16: Status line blank on errors for the tree too

**What to build:** The status line shows no value for the selected tree node while the text has errors, as it already does for the editor cursor. A misplaced comment above the term-matching helper moves back to the method it describes.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] With errors in the text, selecting a term in the Tree pane shows no value in the status line.
- [x] A test covers the tree path with errors, and a test covers a good rule.
- [x] The comment sits above the method it describes.
- [x] `docs/tui.md` still matches the behavior.
