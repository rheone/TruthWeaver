# Terminal UI workbench

**Status:** planned 2026-10-11; blockers cleared (tool-platform C1 to C4 and feature-backlog 16 and 20 are done). Tickets 01 to 08 are done (2026-10-17). Replaces the placeholder ticket `t1` there.

## Purpose

A terminal workbench for rule authors. First priority: a full authoring environment for a rule. Second: exploring and debugging a rule, and teaching Strong K3 (flip a term to `Unknown` and watch `OR` and `NOT` respond).

## Decisions (owner, 2026-10-11)

| Topic | Decision |
| --- | --- |
| Packaging | Separate projects `TruthWeaver.Tool.Tui` and `TruthWeaver.Tool.Tui.Tests`, shipped as their own dotnet tool `truthweaver-tui`. A thin front end over `TruthWeaver.Tool.Core` (see [tool-platform](../tool-platform/spec.md)), like the CLI. The plain CLI stays free of the UI dependency. The TUI is a real tool with roughly the same functionality as the CLI. |
| UI library | Terminal.Gui (tool project only). The worker first checks its current stable status, license and AOT support and reports before adding it. |
| Predicates | Stub values only (a value per term), as in tickets 16 and 20. Loading a host registry is ticket 16b. |
| Layout | Linked panes: rule text on the left, structural tree on the right, always in sync; assignments and decision below; a switchable view pane. Text with a syntax error keeps the last valid tree and marks the error. Panes resize and can be maximised. |
| Import and export | Open text, JSON or YAML with detection; convert between them losslessly (same tree; notes and same-format layout kept); export any artifact (rule text, JSON, YAML, every equation dialect including MathJax, Mermaid, DOT, HTML, truth tables, statistics, diagnostics, report, images when tools exist), export all with a manifest; validate with every level and statistics; clipboard in and out. |
| Version 1 includes | Edit with live diagnostics; assign and decide; explain; views; structural edits and the simplifying rewrites; loading rules inside larger JSON or YAML documents; rule-test integration. |
| Embedded rules | Pick the rule by a path or by browsing a tree. Save by patching only the rule's own span in the document (keeps comments and layout), or by a full round-trip write-back (warns that YAML comments and layout may be lost). Clipboard paste and copy of a valid expression. |
| Mutation | Structural edit operations on the tree (replace operator, wrap in NOT, negate, insert, delete, swap operands, replace a term from a palette, change a literal), with undo and redo; plus the rewrites (Simplify, Canonicalize, NNF, CNF, DNF, compress, expand). Automatic mutant generation is not in v1. |
| Palette | Predicates from the optional YAML schema file, from the open document, and the `TruthWeaver.Predicates` catalog as templates (name, arguments, kinds). Operators and constants are always available. |

## Not in v1

Multi-file projects, schema-driven completion, version history, automatic mutants, host-registry loading (16b), web version (19).

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [01](issues/01-shell-and-linked-editor.md) | Shell, text editor and canonical view | C1, C2, 16 |
| [02](issues/02-assign-decide-explain.md) | Assign term values, decision and trace | 01 |
| [03](issues/03-view-pane.md) | View pane: equations, Mermaid, DOT, HTML | 01 |
| [04](issues/04-structural-editor-and-palette.md) | Structural tree editor, palette and clipboard | 01, 02 |
| [05](issues/05-rewrite-playground.md) | Rewrites with steps and compare | 04 |
| [06](issues/06-embedded-rules-in-documents.md) | Rules inside JSON and YAML documents | 01, C4 |
| [08](issues/08-import-export-and-validation.md) | Import, convert, export and validation panel | 01, C2, C3 |
| [07](issues/07-rule-test-integration.md) | Save cases and run rule tests | 02, 20 |
| [09](issues/09-move-to-terminal-gui-editor-package.md) | Replace the obsolete `TextView` with `Terminal.Gui.Editor` (approved 2026-10-17) | none |
| [10](issues/10-palette-predicates-catalog.md) | Offer the `TruthWeaver.Predicates` catalog in the palette (approved 2026-10-17) | none |
| [11](issues/11-what-would-make-this-true-panel.md) | What would make this True panel | 20 |
| [12](issues/12-apply-a-rewrite-to-the-editor.md) | Apply a rewrite to the editor | none |
| [13](issues/13-compare-two-rules.md) | Compare two rules: looser, tighter, overlap | none |
| [14](issues/14-coverage-over-saved-cases.md) | Coverage over saved cases | none |
| [15](issues/15-definitions-in-the-tree-pane.md) | Definitions in the tree pane | none |
| [16](issues/16-basic-mouse-support.md) | Basic mouse support | none |
| [17](issues/17-color-independent-value-marks.md) | Color-independent value marks and `NO_COLOR` | none |
| [18](issues/18-light-and-dark-theme.md) | Light and dark theme | 17 |
| [19](issues/19-persist-theme-and-layout.md) | Persist the theme and the pane layout | 18 |
| [20](issues/20-debounced-background-diagnostics.md) | Debounced, background diagnostics | none |

Tickets 11 to 15 were the "later" list (counterfactuals, rewrite accept, comparison, definitions, coverage). Not ticketed: rule library references (feature-backlog 11b, on hold) and rule-set analysis (21c, not yet implemented).
