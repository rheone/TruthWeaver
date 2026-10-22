# 06: Fallback keys for keys terminals swallow

**What to build:** A user in Windows Terminal, over SSH or in tmux can reach every command with a key that arrives. Some terminals take Alt+arrows and Ctrl+PageUp/PageDown, and many send no Ctrl+F-keys or Shift+F11/F12. Each affected command gets a second key that works there, and the key reference says which to use where.

**Decisions (settled by grilling):**
- Each affected command gets one second key built from Ctrl or Alt and a letter. Keys are not configurable. Both keys show in the menu and the key reference.

**Blocked by:** 05 (Command keys never lost to the focused pane).

**Status:** done

- [x] The affected commands are listed with the terminals tried, and each has a second key that arrives in them. (The
      terminals are named from documentation only - Windows Terminal, SSH, tmux - not tried in this session; see
      `docs/tui.md#terminals-that-swallow-keys`.)
- [x] A command with two keys shows both in the menu and the key reference, and both run it.
- [x] The key-clash test of ticket 05 also covers the new keys.
- [x] `docs/tui.md` has a short section on terminals that swallow keys.
