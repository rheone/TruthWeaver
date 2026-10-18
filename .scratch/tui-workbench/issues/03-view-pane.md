# 03: View pane: equations, Mermaid, DOT, HTML

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** A switchable pane showing the rule as: tree, equation (every dialect), Mermaid text, DOT text, HTML text, JSON and YAML. Toggles for operator style, constant style (with overrides) and simple variables; render the current decision in the Mermaid, DOT and HTML views where the printers support it. Copy to clipboard and save to a file. Uses the printers and the `TextWriter` overloads.

**Blocked by:** 01

**Status:** done

- [x] Every printer has a view; output equals the library printer's output for the same options (tested)
- [x] Copy and save work; a view that cannot print (for example rule text with `CStyle`) shows the reason
- [x] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
