# 04: TruthWeaver.Tool.Tui README

**What to build:** `src/TruthWeaver.Tool.Tui/README.md` with a `## Use` section: install (`dotnet tool install --global TruthWeaver.Tool.Tui`), how to open a rule file, what the screen shows, a table of key bindings, and a link to `docs/tui.md`. Key types and the live-diagnostics, undo and redo, format and linked canonical view features. List only the references that exist when the work lands; the later Predicates reference and Terminal.Gui.Editor change update this README in their own tickets.

**Blocked by:** 01

**Status:** ready

- [ ] Every key binding matches the code and `docs/tui.md`
- [ ] Example blocks carry doctest markers and pass `dotnet test`
- [ ] The drift test passes for this project
