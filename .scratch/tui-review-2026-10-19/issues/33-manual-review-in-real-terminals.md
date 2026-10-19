# 33: Manual review in real terminals

**What to build:** A person tries the workbench in real terminals and records what works. The tests drive the views without a terminal, so they cannot show that mouse movement, Alt+letter keys, function keys and dialogs behave in a real one. This ticket collects the checks that only a person at a terminal can make, and turns each defect into a ticket.

**Decisions to make during the review:**
- Keep Alt+C for Editor wider, or move it? It is also the Copy hotkey of the export dialog. No free Alt letter is left, so a move needs a new key scheme.
- Which terminals does the documentation name as supported for the hover box and for the fallback keys?

**Blocked by:** None (can start immediately). Needs a person.

**Status:** needs-human-review

- [ ] Hover: in Windows Terminal, Visual Studio terminal, an SSH session and tmux, rest the pointer on an editor word and on a Tree pane line. Record which terminals show the box and which send no mouse movement.
- [ ] Inspect key (Ctrl+F8 and Alt+M): the box opens in each terminal, Esc closes it, and the cursor right after a word is described.
- [ ] Fallback keys: Alt+W, Alt+Q, Alt+Z, Alt+C (panes and dividers), Alt+G, Alt+U, Alt+P, Alt+D, Alt+S, Alt+N, Alt+B, Alt+O, Alt+J, Alt+K and Alt+Y arrive and run their commands in each terminal. Note any key that the terminal or tmux takes.
- [ ] Primary keys that terminals may swallow: Ctrl+PageUp, Ctrl+PageDown, Alt+arrows, the Ctrl+F-keys and Shift+F11 or Shift+F12.
- [ ] Alt+C: with the export dialog open, Alt+C copies. With the workbench focused, Alt+C moves the divider. Record any case where one leaks into the other.
- [ ] List dialogs by mouse: a double-click on an entry chooses it, on a group header or blank space does nothing, and typing in the search box keeps a highlight.
- [ ] Export dialog: the layout fits in an 80 by 24 terminal and in a taller one, the image note shows when a renderer is missing, and Enter in the Types list moves on.
- [ ] Generate rule dialog: the tick boxes work by mouse and keyboard, and OK with nothing ticked keeps the dialog open.
- [ ] Light and dark themes look right in each terminal, with and without `NO_COLOR`.
- [ ] Each defect is written as a new ticket under this folder, and the Decisions above are answered in this file.
