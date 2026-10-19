# 13: Fallback keys that really arrive

**What to build:** Every fallback key from ticket 06 reaches the workbench in a terminal. Ctrl+I is the Tab byte, Ctrl+J is the Enter byte, Ctrl+L is the redraw key of terminals and tmux, and Ctrl+W closes a tab in some hosts, so the four commands that use them (Next pane, Editor narrower, Editor wider, Previous pane) get Alt letters instead. Tests prove each second key runs its command.

**Decisions (settled by grilling):**
- Replace the four keys with Alt plus a free letter that clashes with no menu hotkey (F, E, I, R, A, X, V, H), no export dialog button (C, T, L), no editor, list or text-field binding and no other command. No Ctrl+Shift letters.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The four commands have Alt-letter second keys, shown in the menu and the key reference.
- [ ] The clash test also rejects a command key that a terminal sends as another key (Tab, Enter, Ctrl+L, Ctrl+W, Esc, Backspace and the Ctrl+letter that equal them), so this class of mistake fails the build.
- [ ] A test runs every second key and checks that its command ran (Inspect checks that the box opens).
- [ ] A test presses Ctrl+F12 with the editor focused and checks that Save into the document runs.
- [ ] `docs/tui.md` and the Tui README show the new keys.
