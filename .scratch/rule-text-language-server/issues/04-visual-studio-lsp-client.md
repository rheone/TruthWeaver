# 04: Visual Studio LSP client extension

**What to build:** A new, unpublished Visual Studio extension (likely under a new `editors/visual-studio/` directory, mirroring `editors/vscode/`'s layout) registering `TruthWeaver.LanguageServer` (ticket 02) via `Microsoft.VisualStudio.LanguageServer.Client`. Visual Studio has no TextMate-grammar equivalent, so there is no static fallback layer: coloring, diagnostics and hover all come from the server once it attaches. A file opens uncolored until then; this asymmetry with VS Code is accepted, not worked around with a second classifier.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] Opening a `.truthweaver`/`.tw` file in Visual Studio registers the language and spawns/attaches to the server
- [ ] Semantic tokens color the file once the server attaches
- [ ] Diagnostics from the server appear as Visual Studio error/warning annotations, live and debounced
- [ ] Hovering an operator shows its description from `OperatorDefinitions`
- [ ] No separate static classifier (`IClassifierProvider`/`ITaggerProvider`) is added for fallback coloring
- [ ] The extension remains unpublished: no Marketplace listing
- [ ] A README under its folder documents setup and the accepted pre-attach coloring gap, mirroring `editors/vscode/README.md`'s style
