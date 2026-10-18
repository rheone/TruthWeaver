# 01: Shell, text editor and canonical view

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** New projects `TruthWeaver.Tool.Tui` and `TruthWeaver.Tool.Tui.Tests`, packed as the dotnet tool `truthweaver-tui` (not published). Terminal.Gui application shell with the linked-pane layout (resizable, maximise a pane), a rule text editor with live compile and inline diagnostics (spans, severity), undo and redo, open and save (text, JSON, YAML selected by extension), format on a key (`PrintRuleText` canonical and pretty), and a pane showing the canonical text. Keyboard first, mouse supported, a key help overlay. The text-to-tree sync keeps the last valid tree on a syntax error and marks the error. Core logic (document model, sync, commands) sits in a UI-free library so tests do not need a terminal.

**Blocked by:** C1, C2, 16 (tool-platform and feature-backlog)

**Status:** done

- [x] The UI-free core has tests for compile-on-change, error retention, undo and redo, and format
- [x] The Terminal.Gui layer has smoke tests that run headless where the library allows it
- [x] Terminal.Gui status, license and AOT notes are reported before the package is added, and recorded in `docs/tui.md`
- [x] Packaging, README and the CI pack check include the new tool; nothing is published
- [x] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
