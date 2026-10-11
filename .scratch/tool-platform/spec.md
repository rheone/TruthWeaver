# Tool platform: shared core, CLI and import/export

**Status:** grilled 2026-10-11, ready for implementation

The `truthweaver` command line and the terminal UI are real tools, not supplements. They have roughly the same functionality and share one UI-free core. This spec covers the core, the library additions it needs, and the CLI. The TUI is in [tui-workbench](../tui-workbench/spec.md). It replaces the CLI scope of ticket 16 in [feature-backlog-2026-10-11](../feature-backlog-2026-10-11/spec.md).

## Decisions (owner, 2026-10-11)

| Topic | Decision |
| --- | --- |
| Projects | `TruthWeaver.Tool.Core` and `TruthWeaver.Tool.Core.Tests` (UI-free, not published as a package), `TruthWeaver.Tool` and `TruthWeaver.Tool.Tests` (CLI, `truthweaver`), `TruthWeaver.Tool.Tui` and `TruthWeaver.Tool.Tui.Tests` (`truthweaver-tui`). Front ends are thin; all behavior lives in Core. |
| Dependencies | `System.CommandLine` in the CLI project only (status checked first). Terminal.Gui in the TUI project only. Core takes no UI or CLI dependency. |
| Lossless conversion | Text, JSON and YAML convert to each other with the same compiled tree, guaranteed by a cross-format property test. Same-format saves patch only what changed and keep comments, order and layout. |
| Node notes | New optional per-node `note`: `// note` and `/* note */` in rule text, a `note` property in JSON and YAML. Round-trips through all three formats. Does not affect compile, equality or evaluation. Rewrites drop it. Grammar change with an ADR. |
| Truth tables | `PrintTruthTable` in the library: K3 3^n with a classical 2^n option; default cap 8 terms, raisable with a warning; Markdown, CSV, JSON, HTML, LaTeX; `TextWriter` overload. |
| Validation | One `validate` with every level on by default, each reported separately and switchable: syntax and compile, JSON schema, limits, strict schema file, analysis lints, conversion self-check. Reports statistics: node count, depth, terms, predicates, operator histogram, variables and data sources used, BDD size, tautology or contradiction status, truth-table size. Non-zero exit on errors; `--strict` also on warnings. |
| Images | Optional, through external tools (Graphviz `dot`, Mermaid CLI) when installed; a clear message when missing. No bundled renderer. |
| Tool behavior | Deterministic output (stable order, LF) with `--check`; `export --all <folder>` with a manifest; a `truthweaver.json` config file for defaults; `--json` on every command; shell completion from System.CommandLine. |
| Reports and CI | Markdown report (rule, MathJax equation, diagram, truth table within the cap, statistics, analysis); diagnostics as text, JSON and SARIF; rule tests as JUnit XML. |
| Registry | Stubs only; permissive inference with an optional YAML schema file. Loading a host assembly is ticket 16b (on hold). |

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [L1](issues/L1-node-notes.md) | Node notes in rule text, JSON and YAML | None |
| [L2](issues/L2-print-truth-table.md) | `PrintTruthTable` | None |
| [L3](issues/L3-cross-format-round-trip.md) | Cross-format round-trip guarantee | L1 |
| [C1](issues/C1-core-foundation.md) | Core: load, detect, convert, config, deterministic output | L3 |
| [C2](issues/C2-core-validation-and-statistics.md) | Core: validation levels, statistics, diagnostics writers | C1 |
| [C3](issues/C3-core-export-and-reports.md) | Core: artifact export, export-all, report, images | C1, L2 |
| [C4](issues/C4-core-source-preserving-patcher.md) | Core: same-format patching that keeps comments and layout | C1, L1 |
| [16](../feature-backlog-2026-10-11/issues/16-truthweaver-dotnet-tool.md) | The `truthweaver` CLI over Core | C1, C2, C3 |
| [20](../feature-backlog-2026-10-11/issues/20-declarative-rule-tests.md) | Declarative rule tests (runner in `TruthWeaver.Testing`, JUnit in Core) | 16 |

The TUI tickets (blocked by C1 to C4 and 16) are in [tui-workbench](../tui-workbench/spec.md).
