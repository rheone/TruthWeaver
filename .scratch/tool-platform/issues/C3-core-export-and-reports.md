# C3: Core: artifact export, export-all, report, images

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** An artifact catalog: rule text (word, symbolic, pretty), JSON, YAML, equations in every dialect and constant style (including MathJax and LaTeX wraps), plain text tree, Mermaid, DOT, HTML, truth table (L2) in every format, statistics, diagnostics, decisions (when ticket 02 exists). Export one artifact to a file, standard output or a writer; export all to a folder with a `manifest.json` (artifact id, file, format, options, a hash of the source rule). Markdown report: the rule, the equation as MathJax, a diagram, the truth table (within the cap, otherwise a note), statistics and analysis. Optional image rendering: `--render svg|png` calls Graphviz `dot` or Mermaid CLI if found on the PATH, and reports exactly what is missing if not; no bundled renderer. Output is deterministic.

**Blocked by:** C1, L2

**Status:** done

- [x] Every artifact in the catalog has an export test that equals the library printer's output
- [x] Export-all writes the manifest and is deterministic (run twice, compare)
- [x] The report golden file is tested; image rendering is tested with a fake external tool on the PATH, including the missing-tool message
- [x] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11, dispatch run: done. Decisions taken without the owner (review welcome):
  - Public API in `TruthWeaver.Tool.Core`: `ArtifactCatalog` (`All`, `TryFind`, `Equation(dialect, constants, latexWrap)`, `TruthTable(format)`, `PrettyWidth`), `ArtifactDefinition` (`Id`, `FileName`, `Format`, `Description`, `Options`), `ArtifactExporter` (`Render` by id or definition, `Export` to a `TextWriter` or a path, `RenderAll` -> `ExportBundle`, `ExportAll` -> `ExportManifest`, `HashOf`), `ExportOptions` (`Settings`, `AllowLargerTruthTable`, `Images`), `ExportManifest` (`ToJson`, `FileName` = `manifest.json`), `ExportManifestEntry`, `SkippedArtifact`, `ExportedFile`, `ExportBundle`, `RuleReport.Format`, `ImageRenderer` (`Render`, `FindProgram`, `ProgramName`, `Extension`), `ImageRenderOptions` (`Format`, `SearchPath`, `Timeout`), `ImageFormat`, `DiagramLanguage`. `ValidationReportWriter.FormatStatistics` is now public (the statistics artifacts).
  - Each catalog artifact has fixed printer options (ids like `equation-latex-mathjax-letters`, `truth-table-csv`); the settings give only the limits and the schema file. A front end maps its `--dialect`/`--constants` options to an id through `ArtifactCatalog.Equation`. Operator-style variants exist only for rule text (`rule-text-symbolic`); trees and diagrams use the printer defaults.
  - Decisions are not in the catalog: ticket 02 (serializable decision record) is not done, and a decision needs stub values.
  - No truth-table cap in `truthweaver.json`: the cap is raised per call by `ExportOptions.AllowLargerTruthTable`. Adding a config key is left for ticket 16 if wanted.
  - Export-all skips a truth table over the cap with a new `TWT0015` warning and a `skipped` manifest entry, instead of failing. A rule that does not compile writes nothing. New codes `TWT0016` `RendererNotFound`, `TWT0017` `RenderFailed`, `TWT0018` `UnknownArtifact` (all exit code 2).
  - Manifest hash: SHA-256 of `LoadedRule.Text` with LF line endings (`sha256:<hex>`). The manifest and the report hold no absolute path and no time (the report names the file name only).
  - Images: `dot -T<fmt> -o <out> <in>` and `mmdc -e <fmt> -o <out> -i <in>`, the input in a temp file; on Windows `.exe`/`.cmd`/`.bat` are looked for. Tests use fake `.cmd`/shell-script renderers on an injected search path (`ImageRenderOptions.SearchPath`). Image bytes are as deterministic as the external program.
