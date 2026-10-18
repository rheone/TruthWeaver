# Project organization plan

Status: done 2026-10-18 for Tool.Core (9db9d31), Tool.Tui (00d1248) and Testing (e61cacc). Abstractions, Predicates and AspNetCore stay flat. Written 2026-10-18.

## Rule

A namespace matches its folder path under the project root namespace. This is the convention the main `TruthWeaver`
project already follows (`Evaluation/`, `Parsing/`, `Printing/`, and so on). Enforce it with IDE0130 at warning level in
`.editorconfig` so it cannot drift. The packages are not on NuGet yet, so renaming namespaces breaks no outside consumer.

## Which projects

| Project | Files at root | Decision |
| --- | --- | --- |
| TruthWeaver.Tool.Core | 107 | Split into folders and namespaces. |
| TruthWeaver.Tool.Tui | 85 | Split into folders and namespaces. |
| TruthWeaver.Testing | 41 | Split into folders and namespaces. |
| TruthWeaver.Abstractions | 29 | Leave flat. One cohesive vocabulary. Revisit only if it grows. |
| TruthWeaver.Predicates | 20 | Leave flat. Already grouped by partial files. |
| TruthWeaver.AspNetCore | 13 | Leave flat. |

A flat project needs no folders, so the rule holds for it already.

## Proposed folders

Tool.Core:

- `Export/`: ArtifactCatalog, ArtifactDefinition, ArtifactExporter, ArtifactSource, ExportBundle, ExportManifest, ExportManifestEntry, ExportOptions, ExportedFile, SkippedArtifact, DiagramLanguage, ImageFormat, ImageRenderOptions, ImageRenderer.
- `Diagnostics/`: DiagnosticCodeInfo, DiagnosticCodeTable, DiagnosticDocument, DiagnosticOutputFormat, DiagnosticWriter, SeverityPolicy, ToolDiagnosticCodes.
- `Documents/`: DocumentNode, DocumentNodeKind, DocumentOutline, DocumentPath, DocumentPatch, DocumentRewrite, RuleDocument, SourceNode, SourceNodeIndex, SourceNodeKind, TreeValue, ValuePatch.
- `Tree/`: RuleTree, RuleTreeEdit, RuleTreeEditor, RuleTreeNode, RuleTreeNodeKind, TreeSchemaValidator.
- `Patching/`: RulePatcher, RulePatchKind, RulePatchOutcome, PatchText, RuleEdit.
- `Rewriting/`: RuleRewrite, RuleRewriteKind, NotedRewrite, RewritePlayground, RewritePreview.
- `Assignment/`: AssignmentOutcome, AssignmentSession, AssignmentVariables, StubAssignment, StubEvaluation, StubEvaluationOptions, StubEvaluator, TermAssignment, VariableUse.
- `Palette/`: PaletteArgument, PaletteEntry, PaletteSource, RulePalette.
- `Loading/`: CheckedText, DataSourceFiles, EmbeddedRuleReader, FormatDetection, FormatDetector, FormatEvidence, JsonLogicImporter, LoadedRule, PredicateSchemaFile, RuleLoadOptions, RuleLoader, SourceEncoding, SourceReader, SourceText.
- `Validation/`: RuleValidator, ValidationLevelReport, ValidationLevels, ValidationOptions, ValidationReport, ValidationReportWriter, ValidationStatus.
- `Reports/`: RuleReport, RuleStatistics, RuleComparison, RuleConstancy, RuleTermList, RuleSelection, TraceLine, TraceReasons.
- `Output/`: OutputCheck, OutputCheckStatus, OutputWriter, PrintedRule, DefinitionsPrinter, RuleConverter, WriteBackMode.
- `Configuration/`: ToolConfiguration, ToolConfigurationFile, ToolSettings, ToolTheme, ToolExitCode, ToolResult, ToolResults.
- `Testing/`: RuleTestCaseSpec, RuleTestCaseText.

Tool.Tui (the Workbench partials stay together, in `Workbench/`):

- `Workbench/`: Workbench*.cs, WorkbenchCommand(s), WorkbenchPanes, WorkbenchPreferences, WorkbenchServices, WorkbenchWindow, WorkbenchAppearance.
- `Panes/`: every `*Pane.cs`, IWorkbenchPane, PaneLayout, PaneSlot.
- `Views/`: every `*View.cs`, RuleViewRenderer, ViewKind, ViewOutput, ViewSettings.
- `Controllers/`: every `*Controller.cs`.
- `Commands/`: every `*Commands.cs`.
- `Sessions/`: AssignmentText, EmbeddedRuleSession, RuleTestSession, RuleTestRow, ImportedRule, ExportEntry, CompareLine, CounterfactualLine, ValidationLine, RuleImport, ConversionNotice, CaseCoverage.
- `Theme/`: ColorMode, ThemeChoice, ThemePalette, ThemeVariant, TruthColors, TerminalBackground, EditorMarks.
- `Terminal/`: IClipboardAccess, TerminalClipboard, TerminalDialogs, TerminalPrompts, IWorkbenchDialogs, IWorkbenchPrompts, TuiStartup, TuiStartupResult.
- `Predicates/`: CatalogPredicate, PredicateCatalog.
- `Program.cs` and `Results.cs` stay at the root.

Testing (public package): `Assertions/` (DecisionAssertions*, RuleAssertions, RewriteAssertions, RewriteExpectations, exception types), `Fakes/` (FakePredicates, FakeDataSource, StubRuleCompiler, StubValues), `Harness/` (PredicateHarness*, HarnessValues, ArgumentReadTracker, SimulatedPredicateFaultException), `Fuzzing/` (RuleFuzz*, FuzzAssignment, GeneratedRule, K3Oracle, K3RuleGenerator), `RuleTests/` (RuleTest*).

These lists are a first cut by file name. Check each against its real use before moving it, and ask the owner about any file that fits two folders.

## Cost and risk

- Public type names move: the `Testing` namespace splits, so the samples, the docs and the doctests that show `using TruthWeaver.Testing;` change. `docs/` and `README.md` examples are tested, so a missed one fails `dotnet test`.
- Tool.Core and Tool.Tui tests use `InternalsVisibleTo`. Mirror the folders in the test projects, and update the architecture tests that pin namespaces.
- A move with `git mv` keeps history. Do it as one commit per project, with no logic edits in the same commit.
- Docs with code: update any page that names a moved namespace or path, and the project READMEs (the drift test `ProjectReadmeTests` checks them).

## Order

1. Add IDE0130 as a suggestion, then raise it to a warning per project as each project is done.
2. Tool.Core (largest, no outside consumers).
3. Tool.Tui.
4. Testing, with the samples and docs.
5. Run the full validation from `CLAUDE.md` after each project.

## Open questions

- Does the owner want Abstractions split too (for example `Data/`, `Tracing/`, `Predicates/`)? Default: no.
- Do the Tool.Core `Testing/` folder and the Testing package name clash in reading? Alternative folder name: `RuleCases/`.
