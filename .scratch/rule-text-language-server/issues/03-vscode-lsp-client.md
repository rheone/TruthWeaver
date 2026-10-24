# 03: VS Code LSP client extension

**What to build:** Extend `editors/vscode/` with an LSP client (`vscode-languageclient`) that spawns and talks to `TruthWeaver.LanguageServer` (ticket 02). The existing TextMate grammar (`syntaxes/truthweaver.tmLanguage.json`) is kept unchanged as the always-on base coloring layer; semantic tokens from the server layer on top once it attaches, refining specific spans. This extension is not published (same posture as today).

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] Opening a `.truthweaver`/`.tw` file still colors instantly via the existing grammar, with no server dependency for that base layer
- [ ] Once the server attaches, semantic tokens visibly refine coloring without fighting the grammar's base colors
- [ ] Diagnostics from the server appear as editor problems/squiggles, live and debounced
- [ ] Hovering an operator shows its description from `OperatorDefinitions`
- [ ] The extension remains unpublished: no Marketplace/Open VSX listing, `"private": true` retained
- [ ] `editors/vscode/README.md` documents the new client capability alongside the existing grammar description
