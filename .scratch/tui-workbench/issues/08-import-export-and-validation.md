# 08: Import, convert, export and validation panel

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** The TUI front end for `TruthWeaver.Tool.Core`'s import, conversion, export and validation. Open a file, standard input or the clipboard in any of text, JSON or YAML (detected, with override). Convert the open rule between the three forms with a key (same tree; notes kept; a banner states when something cannot be kept). An Export menu lists every artifact in the Core catalog: rule text in each operator style, JSON, YAML, each equation dialect and constant style (including MathJax and LaTeX wraps), plain text tree, Mermaid, DOT, HTML, truth table in each format (with the cap warning and the raise option), statistics, diagnostics (text, JSON, SARIF), the Markdown report, and images when Graphviz or Mermaid CLI are installed; choose a file, copy to the clipboard, or export all to a folder with a manifest. A validation panel runs every level, shows each level's result and the statistics, and jumps to the offending span; strict mode toggle. Clipboard paste of a valid expression in any of the three forms.

**Blocked by:** 01, C2, C3 (tool-platform)

**Status:** on-hold until the tool-platform Core tickets and tickets 16 and 20 are done; then ready-for-agent

- [ ] Every Core artifact is reachable from the Export menu and the output equals Core's (tested through the UI-free layer)
- [ ] Conversion between the three forms keeps the compiled tree equal and notes intact (tested)
- [ ] The validation panel shows all levels and statistics, and selecting a diagnostic moves the cursor to its span
- [ ] A missing image tool is reported with what to install; no dialog blocks the editor
- [ ] The page for this feature is written or updated in the same change (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
