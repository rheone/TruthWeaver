# 10: CLI, export catalog, tree and report use the printing module

**Status:** done
**Type:** task
**Blocked by:** 09
**Source:** architecture review of 2026-10-19, candidate 3

**What to build:** The CLI `format` and `convert` commands, `ArtifactCatalog`, `RuleTree` and `RuleReport` call the printing module. The two JSON indenting paths become one.

- [x] CLI and workbench produce the same text for the same rule and options
- [x] `ExportParityTests` pass
- [x] The CLI reference page is updated if any output changes
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

- `RulePrinter.Print` gained `includeNotes` (default true) and now owns the rule text fallback to the word style when the operator style has no rule text that reads back (formerly copied in `RuleTree.PrintText`).
- `RuleTextPrinting` (CLI `format`, `convert`, `import`), the five rule artifacts of `ArtifactCatalog`, `RuleReport` and `RuleTree.PrintText` call `RulePrinter`.
- One JSON indenter: `RulePrinter.IndentJson(JsonNode)` (backed by `DiagnosticWriter.WriteJson`); `CommandOutcome.FormatJson` and the string overload delegate to it.
- Output is unchanged for supported settings. One difference: the CLI with a C-style operator setting prints word-style rule text instead of throwing.
- Not changed: `DefinitionsPrinter` keeps its own fallback copy, `PatchText.Indented` is a separate indenter with a configurable indent size, and `PrintedRule` still prints directly.
