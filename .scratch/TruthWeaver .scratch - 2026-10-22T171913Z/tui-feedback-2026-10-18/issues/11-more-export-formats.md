# 11: More export formats

**What to build:** The export dialog lists every artifact that `ArtifactCatalog` offers, in groups: rule text (canonical, symbolic, pretty), JSON, YAML, tree, Mermaid, DOT, HTML, equations (every dialect with its constant styles and the LaTeX wrap modes), truth tables (Markdown, CSV, JSON, HTML, LaTeX), statistics (JSON and text), diagnostics (text, JSON, SARIF) and the report. The default destination is the clipboard. Mermaid is missing from the list today.

**Blocked by:** 10

**Status:** done

- [x] Every catalog artifact appears in the dialog and exports through the same path as the command line.
- [x] A test fails when a catalog artifact has no entry in the dialog.
- [x] Image formats appear only when the external renderer is found, and the dialog says what to install when it is not.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
