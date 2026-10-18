# 04: TruthWeaver.Tool.Tui README

**What to build:** `src/TruthWeaver.Tool.Tui/README.md` with a `## Use` section: install (`dotnet tool install --global TruthWeaver.Tool.Tui`), how to open a rule file, what the screen shows, a table of key bindings, and a link to `docs/tui.md`. Key types and the live-diagnostics, undo and redo, format and linked canonical view features. List only the references that exist when the work lands; the later Predicates reference and Terminal.Gui.Editor change update this README in their own tickets.

**Blocked by:** 01

**Status:** done

- [x] Every key binding matches the code and `docs/tui.md`
- [x] Example blocks carry doctest markers and pass `dotnet test`
- [x] The drift test passes for this project

## Comments

- The README follows docs/tui.md for the install step (pack from the repository, not on NuGet) and for the key bindings, which match WorkbenchCommands and the command lists. The shell block has a skip marker because it is not a rule. The Predicates reference and Terminal.Gui.Editor change are left to their own tickets.
