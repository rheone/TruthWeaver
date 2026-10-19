# 32: Clash-helper test proves the window-owned keys half

**What to build:** The test of the clash helper shows why a window-owned key (Ctrl+Z, Ctrl+Y) is not reported. Today that half passes only if the editor or list binds the key by itself, and the test does not show it. The test adds a view that binds the owned key and checks that the helper skips it because it is owned, and that the same binding on a key that is not owned is reported.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A test binds a window-owned key in a view and checks that the helper does not report it.
- [ ] A test binds a key that is not window-owned in the same way and checks that the helper reports it.
- [ ] The two tests fail if the owned-key list is emptied (show it during development).
- [ ] All tests pass.
