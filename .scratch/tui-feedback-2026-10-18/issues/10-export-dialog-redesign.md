# 10: Export dialog redesign

**What to build:** The export dialog groups its options under headings, highlights the chosen output type (text, Markdown, AsciiMath and the rest), and uses buttons named Copy, To File and Cancel, so the second dialog is not needed. A double click on an option selects it and does not end the dialog. The truth-table choice is a real selection: today OK switches the table format instead of letting the user pick one. Dropdowns are used where a list does not fit.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] A double click never closes the dialog.
- [x] Copy puts the output on the clipboard, To File asks for a path in the same dialog, Cancel changes nothing.
- [x] The truth-table format can be chosen from every format the tool offers.
- [x] The default action is Copy.
- [x] Tests drive the dialog by keyboard and mouse.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
