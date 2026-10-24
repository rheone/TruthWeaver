# 05: Bundled scoped color theme (light + dark)

**What to build:** A scoped color contribution, not a full standalone theme, that recolors this DSL's grammar scopes and semantic token types on top of whichever theme (light or dark) the user already has — light and dark variants, covering both VS Code's `editor.tokenColorCustomizations`/semantic token color defaults and whatever Visual Studio classification-format equivalent applies once ticket 04 exists. No other editor chrome colors change.

**Blocked by:** 01 (needs the final token-kind taxonomy to know what to color)

**Status:** ready-for-agent

- [ ] Colors are defined for both a light and a dark variant
- [ ] Only this DSL's scopes/semantic token types are recolored; the rest of the user's active theme is untouched
- [ ] The contribution works whether the user's base theme is light or dark (verified against at least one of each)
- [ ] `editors/vscode/README.md` (and, once it exists, the Visual Studio client's README) documents the contribution and how to disable it if a user prefers their theme's own colors
