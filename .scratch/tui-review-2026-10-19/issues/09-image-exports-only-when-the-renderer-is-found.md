# 09: Image exports appear only when the renderer is found

**What to build:** The export dialog lists image formats only when the external renderer is found. When it is missing, the dialog says once what to install, and does not list four entries that cannot run.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] With the renderer found, the image formats are listed and work.
- [ ] With the renderer missing, no image format is listed and one line says what to install.
- [ ] Tests cover both cases without needing the real renderer.
- [ ] `docs/tui.md` describes the current behavior.
