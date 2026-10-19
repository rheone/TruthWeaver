# 21: CLI and TUI export parity

**What to build:** The command line and the workbench offer the same export options, and a test keeps them that way. Both already read `ArtifactCatalog` for the artifact list, but the truth-table term limit (`ExportOptions.TruthTableTermLimit`, added with whats-next ticket 02) has no control in either front end, and nothing fails when one front end gets an option the other lacks. Origin: owner question during the 2026-10-18 dispatch run.

**Blocked by:** 11

**Status:** ready-for-agent

- [x] The command line has an option that sets the truth-table term limit (for example `--truth-table-limit <n>`), shown in `export` and `print` help, and it keeps `--allow-large-truth-table` working (it means the default limit of 12).
- [x] The export dialog has a control for the same limit, kept for the next export like the "allow larger truth tables" choice.
- [x] Every artifact id that the catalog offers is accepted by the command line `--artifact` option and listed in the export dialog. A test fails when either front end lacks one.
- [x] The image formats the dialog offers (Mermaid and DOT as SVG and PNG) match what `export --render` can write, or the difference is listed in the docs.
- [x] Every export option of `ExportOptions` is either exposed by both front ends or listed in a test as deliberately front-end specific (for example the clipboard destination, which only the workbench has).
- [x] `docs/cli.md` and `docs/tui.md` are updated to the current behavior, with no "changed from" text, and each says which export options the other front end has.
