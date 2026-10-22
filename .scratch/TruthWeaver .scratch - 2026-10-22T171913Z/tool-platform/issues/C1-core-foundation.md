# C1: Core: load, detect, convert, config, deterministic output

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** New projects `TruthWeaver.Tool.Core` (UI-free, not published as its own package) and `TruthWeaver.Tool.Core.Tests`. Behavior: load a rule or document from a file, standard input or a string; detect the format (text, JSON, YAML) with an explicit override; handle encoding and BOM; extract an embedded rule by path; convert between the three forms; deterministic output writer (stable order, LF, final newline) with a `--check`-style compare against an existing file; read the `truthweaver.json` configuration (defaults for operator style, constant style, dialect, limits, schema file); the stub-assignment and data-source file loaders from the ticket-16 scope. Every operation returns a result type with diagnostics; no exceptions for expected failures, and a `Try…` sibling for any throwing member. No dependency beyond the existing TruthWeaver packages.

**Blocked by:** L3

**Status:** done

- [x] Each load, detect and convert operation is covered in tests with fixtures, including a BOM file and an embedded rule
- [x] The deterministic writer and the compare are tested (differing and identical files)
- [x] The config file has a JSON schema and a documented precedence (command line over config over defaults)
- [x] Packaging notes: not packed as its own package; the CI build includes it
- [x] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11, dispatch run: done. Decisions taken without the owner (review welcome):
  - Public API in namespace `TruthWeaver.Tool.Core`: `SourceReader`/`SourceText`/`SourceEncoding`, `FormatDetector`/`FormatDetection`/`FormatEvidence`, `RuleLoader`/`RuleLoadOptions`/`LoadedRule`, `RuleConverter` (`CreateCompiler`, `Compile`, `Convert`), `ToolConfigurationFile`/`ToolConfiguration` (one layer, nullable members)/`ToolSettings` (`Resolve(file, commandLine)`, `ToCompilerOptions`), `StubAssignment`, `DataSourceFiles`, `OutputWriter`/`OutputCheck`/`OutputCheckStatus`, `ToolResult<T>` and `ToolDiagnosticCodes`. No member throws for an expected failure, so no `Try` siblings were needed beyond `ToolResult<T>.TryGetValue`; Tool.Core joined the Try-first architecture check.
  - Permissive inference is `CompilationMode.Lenient` over an empty registry (`RuleConverter.CreateCompiler`): any predicate, argument and `from(...)` source compiles, terms evaluate `Unknown`. The compiler's context type is `StubAssignment`, so the stub engine (ticket 20) can register stand-ins over it. Strict schema-file checking stays in C2.
  - Tool diagnostics use a new `TWT` prefix (`TWT0001` to `TWT0011`) in `ToolDiagnosticCodes`, documented in `docs/tool-core.md`, separate from the library's `TRE` codes. C2's SARIF rule ids need to include them.
  - Embedded-rule paths use a small singular JSONPath-style grammar of our own (`.name`, `['key']`, `[n]`, optional `$`), not the Meziantou JSONPath engine, so a path always selects one node. An object/mapping gives the rule's own source text (YAML: a slice re-indented to its column, comments kept; the end is found with YamlDotNet's event parser because representation-model End marks are the start event's end); a string gives rule text. No offsets are exposed yet; C4 owns span patching.
  - YamlDotNet is used directly in Tool.Core through the existing `TruthWeaver.Yaml` reference (no new PackageReference). The test project references the existing central `JsonSchema.Net` to validate `truthweaver.schema.json`.
  - Config: `truthweaver.json`, found by walking up from a start directory; members `operatorStyle`, `constantStyle`, `dialect`, `limits.{maxDepth,maxNodeCount,maxAnalysisTerms,maxRewriteNodeCount}`, `schemaFile` (resolved from the file's directory). Enum values are case-insensitive; the schema lists lower case. An unknown property is warning `TWT0008` (the schema rejects it). `constantStyle` defaults to each printer's own default (null), not one global value. No truth-table cap in the config yet (C3 can add it).
  - Stub-value file: a flat JSON object or YAML mapping from term text to `True`/`False`/`Unknown` (also `T`/`F`/`U`, JSON booleans). Keys are compiled and keyed by canonical text, so spacing is normalized but name case is kept (lenient terms keep the written case). Ticket 20 may wrap this in its `given` block.
  - Format detection: override, then extension (`.json`, `.yaml`/`.yml`, `.rule`/`.tw`/`.txt`), then content. The `.rule`/`.tw`/`.txt` extensions are a choice; no extension for rule text was established before.
  - `OutputWriter.Check` compares bytes exactly (a CRLF file or a BOM differs). Missing and differing files are results, not diagnostics.
  - The BOM fixture is `access-bom.rule`: `TextFileEncodingTests` forbids a BOM in repository `.json`/`.yaml` files, so a BOM JSON fixture was not possible; UTF-16/32 marks are generated in tests.
