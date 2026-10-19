# 06: Fallback keys for keys terminals swallow

**What to build:** A user in Windows Terminal, over SSH or in tmux can reach every command with a key that arrives. Some terminals take Alt+arrows and Ctrl+PageUp/PageDown, and many send no Ctrl+F-keys or Shift+F11/F12. Each affected command gets a second key that works there, and the key reference says which to use where.

**Blocked by:** 05 (Command keys never lost to the focused pane).

**Status:** ready-for-agent

- [ ] The affected commands are listed with the terminals tried, and each has a second key that arrives in them.
- [ ] A command with two keys shows both in the menu and the key reference, and both run it.
- [ ] The key-clash test of ticket 05 also covers the new keys.
- [ ] `docs/tui.md` has a short section on terminals that swallow keys.
