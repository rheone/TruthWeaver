# Changelog

All notable changes to TruthWeaver are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Versioning and release status

> [!IMPORTANT]
> **TruthWeaver has not been published.** No package has been pushed to NuGet, the repository has no release tags, and
> every package version comes from MinVer, so an untagged build is `1.0.0-preview.0.N`. Nothing below
> has been shipped to a consumer yet, so the "breaking changes" are measured against two reference points rather than
> against a released version:
>
> - **The first engine commit, [`df8f4b9`](https://github.com/rheone/TruthWeaver/commit/df8f4b9)**
>   (`BooleanRulesEngine` v1). Anyone who cloned the project early starts here. See
>   [Migrating from the first engine commit](#migrating-from-the-first-engine-commit).
> - **The commit before the Strong K3 work, `57cf2c9`.** The K3 effort changed most of the public surface. See
>   [Migrating across the Strong K3 work](#migrating-across-the-strong-k3-work).

**Version policy.** MinVer derives the version from the Git tags (tag prefix `v`). Until the first release, no tag exists, so every build is a `1.0.0-preview.0.N` pre-release (`N` is the number of commits since the start of the history). Until `1.0.0` ships, any change may break
compatibility, and this file is where each break is recorded with its migration step. From `1.0.0` the packages follow
[Semantic Versioning](https://semver.org/): breaking changes only in a major version, new backward-compatible features
in a minor version, fixes in a patch version. All seven packages share one version. [How a version is made](docs/contributing.md#how-a-package-version-is-made).

**Package metadata**: the seven packages (`TruthWeaver`, `TruthWeaver.Abstractions`, `TruthWeaver.DataSources.Json`, `TruthWeaver.Generators`, `TruthWeaver.Predicates`, `TruthWeaver.Testing`, `TruthWeaver.Yaml`) are packable, carry the Apache-2.0
license expression, the repository URL and commit, symbol packages (`.snupkg`, except the analyzer-only generator package), SourceLink and the repository `README.md` as the
package readme. Each package sets its own `PackageId`, `Description` and `PackageTags`; the release notes link to this file. Open items, deliberately not changed here: there is no `PackageIcon` (no icon asset exists), and the
copyright line reads 2026.

**Consumer requirement.** None. The packages no longer set `EnablePreviewFeatures`, so they carry no requires-preview-features marker and a consuming project needs no preview opt-in (no `CA2252`). This is checked by compiling the README quick-start in a fresh project.

## [Unreleased]

### Added

- `DecisionRecord` (feature backlog 02): a `Decision` stored as JSON for an audit log and read back for replay. `new DecisionRecord(decision, rule, options)` holds the decision, a `RuleIdentity` and the `EvaluationOptions`; `PrintJson(DecisionJsonOptions?)` (with a `TextWriter` overload) writes indented JSON and `DecisionRecord.TryParseJson(json, out record, out error)` reads it back without throwing, with the JSON path of the first problem in `error`. `RuleIdentity.Of(rule, name, version)` holds the SHA-256 hash of the canonical rule text and a caller-supplied name and version. The literal argument values of faulting terms are omitted unless `DecisionJsonOptions.IncludeArgumentValues` is set; the trace is written as the decision holds it. A fault reads back with the new `RecordedFaultException` (type name and message), or as a `VariableResolutionException` for a variable failure. The writer and reader use `Utf8JsonWriter` and `JsonDocument`, not a reflection serializer. The shape is in `src/TruthWeaver/Json/decision-record.schema.json`, which the package carries as `content/decision-record.schema.json`. No migration step: these are new types. `docs/decision-records.md` covers it.
- `RuleCoverage` (feature backlog 04): the coverage of one rule over many decisions, for finding dead branches and unreliable data. `RuleCoverage.FromDecisions(rule, decisions)` and `FromDecisionsAsync(rule, IAsyncEnumerable<Decision>)` read the trace tree of each decision and call no predicate; the output of `EvaluateManyAsync` goes straight into `FromDecisionsAsync`. `Terms` has one `TermCoverage` per distinct term (evaluated, `True`, `False`, `Unknown` and fault counts, the number of decisions it decided, `NeverDecided`, `AlwaysUnknown`), `Nodes` one `NodeCoverage` per node by tree path (with not-evaluated counts and `UndecidedOperands`), and `OperatorsWithUndecidedOperands` lists the branches that never decided per operator. A node decides when a different value there, with the other evaluated nodes kept and skipped nodes read as `Unknown`, changes the result. A decision without a trace tree or from another rule throws `ArgumentException`. `PrintMarkdown` and `PrintJson` (each with a `TextWriter` overload) write the reports. No migration step: these are new types. `docs/rule-coverage.md` covers it.
- `CompiledRule.EvaluateManyAsync` (feature backlog 06): evaluates one rule against an `IEnumerable<TContext>` or an `IAsyncEnumerable<TContext>` and returns an `IAsyncEnumerable<Decision>` in the order of the contexts. `maxDegreeOfParallelism` (default 1, below 1 throws `ArgumentOutOfRangeException`) caps the evaluations in flight; a decision that finishes early waits for the ones before it. The `services`, `dataSources` and `EvaluationOptions` apply to every context, so `Timeout` and `FaultBudget` count per context. A predicate fault affects only its own decision. Cancellation throws `OperationCanceledException`; when the consumer stops reading or an evaluation throws, the evaluations in flight are cancelled and awaited. There is no array form: `ToArrayAsync` from `System.Linq` collects the decisions. A new `BatchEvaluationBenchmarks` compares the method with a `foreach` loop. No migration step: these are new members. `docs/batch-evaluation.md` covers it.
- Tool core write-back of edited rules (tool-platform C4). `RulePatcher.Patch` writes an edited `CompiledRule` back into its rule text, JSON or YAML document, one rule (`LoadedRule`) or several (`RuleEdit<TContext>` list, applied in order so each rule patches independently). The default `WriteBackMode.Patch` replaces only the changed values (operator and predicate names, arguments, constants, notes) in place, so comments, key order, quotes, indentation and every byte outside them stay; YAML positions come from the YamlDotNet event marks, JSON positions from a span-keeping reader. When the change does not fit (structure changed, a YAML block scalar, alias, anchor or tag, or rule text), the rule's own span is written again in the document's style with the new `TWT0019` warning (`RuleRewritten`). `WriteBackMode.RoundTrip` serializes the whole document again with the new `TWT0020` warning (`RoundTripWriteBack`). Every result is read back and compiled; a result that does not read back as the edit, or a change to an anchored rule, is the new `TWT0021` error (`WriteBackFailed`, exit code 2). The result is a `DocumentPatch` (text, original `SourceEncoding`, one `RulePatchOutcome` with a `RulePatchKind` per edit); `RulePatcher.WriteFile` and `Encode` keep the byte order mark and line endings. No migration step. `docs/tool-core.md` covers it.
- `TruthWeaver.Tool.Core` and `TruthWeaver.Tool.Core.Tests` (tool-platform C1): the UI-free core of the `truthweaver` command line and terminal UI. It is not a package (`IsPackable=false`); it is in `TruthWeaver.slnx`, so CI builds, tests and analyzes it. It references only `TruthWeaver`, `TruthWeaver.Yaml` and `TruthWeaver.DataSources.Json`, and new architecture tests check that it takes no command-line, terminal UI or test-support library and that no package references it. It loads a rule from a file, standard input or a string (`RuleLoader`, `SourceReader`): the byte order mark selects UTF-8, UTF-16 or UTF-32 and is removed, input with no mark is strict UTF-8; `FormatDetector` finds text, JSON or YAML from an override, then the extension, then the content; `RuleLoadOptions.Path` selects a rule inside a JSON or YAML document by a singular JSONPath-style path (an object or mapping is a tree rule with its source text, comments included; a string is rule text). `RuleConverter` compiles permissively (an empty registry in `CompilationMode.Lenient`, so any predicate and any `from(...)` source compiles) and converts between the three formats with notes. `ToolConfigurationFile` finds and reads `truthweaver.json` (operator style, constant style, equation dialect, compile limits, schema file), with the JSON schema `truthweaver.schema.json`; `ToolSettings.Resolve` puts the command line over the file over the defaults. `StubAssignment` loads stub values (a truth value per term, keyed by canonical term text) and `DataSourceFiles` loads JSON and YAML data files. `OutputWriter` writes LF, a final newline and UTF-8 without a byte order mark, and `Check` compares a file byte for byte for `--check`. Every operation returns a `ToolResult<T>` with diagnostics; the core reports its own problems with the `TWT0001` to `TWT0011` codes (`ToolDiagnosticCodes`). No migration step: these are new projects. `docs/tool-core.md` covers it, linked from the README and `docs/packages.md`.
- Tool core artifact export, export of every artifact, report and images (tool-platform C3). `ArtifactCatalog` lists every artifact the tools make from a rule, each one library printer with fixed options: rule text (word, symbolic, pretty at 80 columns), JSON, YAML, the equation in every dialect and constant style plus the LaTeX `$$` and MathJax envelopes, the plain text tree, Mermaid, DOT, HTML, the truth table in every format, the statistics (JSON and text, through the new public `ValidationReportWriter.FormatStatistics`), the diagnostics (text, JSON, SARIF) and the report. `ArtifactExporter` renders one artifact to a string, a `TextWriter` or a file, and `ExportAll` writes every artifact to a folder with a `manifest.json` (artifact id, file, format, options, `sha256:` hash of the source rule; no absolute path or time, so the folder is the same on each run); `RenderAll` gives the same files unwritten for `--check`. A truth table over the cap is left out of an export of every artifact with the new `TWT0015` warning (`ArtifactSkipped`) and listed as skipped. `RuleReport` writes the Markdown report: the source, the rule text, the equation as MathJax, a Mermaid diagram, the truth table within the cap (otherwise a note), the statistics and the analysis findings. `ImageRenderer` renders a Mermaid or DOT diagram as SVG or PNG with Graphviz `dot` or the Mermaid CLI `mmdc` found on the `PATH` (no bundled renderer); `ExportOptions.Images` renders both diagrams in an export. New codes `TWT0016` (`RendererNotFound`, the message names the missing program and where to get it), `TWT0017` (`RenderFailed`) and `TWT0018` (`UnknownArtifact`); the three map to exit code 2. No migration step. `docs/tool-core.md` covers it.
- Tool core validation, statistics and diagnostics writers (tool-platform C2). `RuleValidator.Validate` checks a loaded rule in six levels, each switchable through `ValidationOptions.Levels` and reported separately in a `ValidationReport` (`Passed`, `PassedWithWarnings`, `Failed`, `Skipped` with a reason, or `Off`): compile diagnostics, the rule tree JSON schema for JSON and YAML rules (the `rule-tree.schema.json` of the `TruthWeaver` package, embedded and read by a small built-in checker, so Tool.Core takes no JSON schema library), the depth and node limits, the predicate schema file (a new YAML format read by `PredicateSchemaFile`: predicate names, argument names, kinds and required flags), the Strong K3 analysis with the logic and structure lints, and a conversion self-check (rule text, JSON and YAML compile back to the same tree and notes). A rule that fails only a limit is compiled again without the limit, so the other levels and the statistics still run. `RuleStatistics` gives the node count, depth, term count, distinct terms, predicates, operator histogram, variables and data sources, BDD size, tautology or contradiction status (`RuleConstancy`) and the truth-table size against the cap. `DiagnosticWriter` writes diagnostics as text with spans, JSON or SARIF 2.1.0; the SARIF rules come from the new `DiagnosticCodeTable` (every `TRE` and `TWT` code), and a test validates the log against the SARIF 2.1.0 schema. `ValidationReportWriter` writes a whole report. `SeverityPolicy` maps diagnostics to a `ToolExitCode` (`Success` 0, `Failed` 1, `Unusable` 2), and strict mode fails on warnings. New codes `TWT0012` (`InvalidSchemaFile`), `TWT0013` (`TreeSchemaViolation`) and `TWT0014` (`ConversionMismatch`). No migration step. `docs/tool-core.md` covers it.
- `CompiledRule.PrintTruthTable(TruthTableOptions?)` (tool-platform L2), with a `TextWriter` overload and the `Try` forms `TryPrintTruthTable` (string and `TextWriter`), `TruthTableOptions` and `TruthTableFormat`. The table lists every combination of `False`, `Unknown` and `True` for the distinct terms of a rule (3^n rows) and the rule's value for each, or only `False` and `True` with `Classical` (2^n rows). The columns follow the first occurrence of each term, as the letters of `PrintEquationWithLegend` do; the first term changes slowest and the values run `False`, `Unknown`, `True`. The formats are Markdown (the default), CSV, JSON, HTML and LaTeX; terms are calls or letters (`SimpleVariables`), and `Constants` sets the cell text (`Letters` by default). A term is a free variable, so a rule with variable references prints the same way and no predicate runs. The default cap is 8 terms (`TruthTableOptions.DefaultTermCap`, 6,561 rows). A larger table needs `AllowLargerTable` and carries the new `TRE0034` warning (`TruthTableCapRaised`); above the hard limit of 12 terms (`HardTermLimit`, 531,441 rows) or over the cap without the option, the table is refused: the string form throws `InvalidOperationException`, the `Try` form returns `false` with the new `TRE0035` error (`TruthTableTooLarge`), and nothing is written. A property test checks that every row equals `EvaluateAsync` with the same stub values, and a second test checks the connectives against `K3Oracle`. No migration step: these are new members. `docs/truth-tables.md` covers it with an example of each format, and the doc checker has a new `truthtable` marker.
- Cross-format conversion guarantee and helpers (tool-platform L3). `RuleFormat` (`Text`, `Json`, `Yaml`, in `TruthWeaver.Compilation`) names the three formats. `TruthWeaver.Yaml` adds `RuleFormatExtensions`: `compiler.Convert(source, from, to, includeNotes)` with its `Try` form `TryConvert`, returning a `RuleConversionResult` (`Text`, `Diagnostics`, `Succeeded`); `compiler.CompileAs(source, format)` with `TryCompileAs`; and `rule.PrintAs(format, includeNotes)`. A new property test converts generated rules (every operator, every literal kind including number extremes, strings with quotes, backslashes, line breaks and control characters, variable references and notes) in all six directions over 300 seeds and finds an equal tree and equal notes each time; it found no printer or reader gap. No migration step: these are new members. `docs/rule-formats.md` states the guarantee and what a conversion does not keep (source layout and spelling, YAML `#` comments, note text outside the normal form, notes after a rewrite).
- Node notes (tool-platform L1). Any node of a rule can carry a note: a `// note` or `/* note */` comment in rule text, a `note` string property in JSON and a `note` key in YAML (`rule-tree.schema.json` accepts it on every node kind), or `RuleBuilder.WithNote(note)`. The parser attaches each comment to one node: a comment on the same line as the token before it to the smallest node that ends there, otherwise to the smallest node that starts at the next token, otherwise to the node before it or the node around it. `CompiledRule.Notes` (an `IReadOnlyDictionary<string, string>`) holds the notes, keyed by tree path (`$`, `$.operands[1]`). Notes are not part of the tree: a rule with notes compiles to an equal tree, the same `CanonicalText` and diagnostics, and evaluates the same way. `PrintRuleText`, `PrintJson` and `PrintYaml` write the notes by default, so a conversion between the formats keeps them; `RuleTextOptions.IncludeNotes = false`, `PrintJson(includeNotes: false)` and `PrintYaml(includeNotes: false)` (each with a `TextWriter` overload) leave them out, and `CanonicalText` never has them. Rewrites return rules with no notes; `RuleBuilder.FromCompiled` keeps them. `RuleText.NormalizeWhitespace` keeps comments and ends a `//` comment with a line feed. ADR-0005 decision 2 is amended in place. Migration: rule text that already compiled had no `/` in it, so it compiles as before. `PrintRuleText()` with no arguments now returns `CanonicalText` only for a rule with no notes. A JSON or YAML rule that used a `note` key was rejected before and now compiles. `docs/rule-formats.md` (Notes), `docs/rule-text.md` and `docs/rulebuilder.md` cover it.
- `NullBehaviorScope` and `PredicateFamily` (followups A): a host sets the `NullBehavior` default once for every built-in predicate factory call it makes inside a scope, `using (NullBehaviorScope.Begin(NullBehavior.False)) { ... }`, or for one family, `NullBehaviorScope.Begin(PredicateFamily.Numeric, NullBehavior.False)`. `NullBehavior` gains the member `Default`, which is now the default value of every factory's `nullBehavior` parameter and means "follow the open scope, or `Unknown` when none is open"; a `nullBehavior` value passed to a factory always wins over a scope, a family scope wins over an all-families scope, and `Unknown` stays the default. `IsNull`/`IsNotNull` and faults are not affected. The setting is a scope and not a `PredicateRegistryBuilder` property because the factories return their schema and delegate before the builder sees them, and the core package does not reference the predicates package. Migration: none for code that passes no option. A `switch` over `NullBehavior` with no default arm needs an arm for `Default` (a factory never evaluates with it). A host that passed `NullBehavior.Unknown` on every call to be explicit keeps that behavior and does not follow a scope. `docs/predicates.md`, `docs/predicate-conventions.md` and `docs/strong-k3/predicates/README.md` cover it, including a table of when each predicate group answers `Unknown`.
- `PredicateSchema.Cost` (an optional `double?`, set with `init`): a relative cost hint for a predicate, unset (the default, "neutral") by every existing registration, so nothing changes until a host sets it. `CompiledRule.OptimizeOrder()` (with its `Try` sibling `TryOptimizeOrder`) and `OrderOptimizationOptions` are new: `OptimizeOrder()` stable-sorts the operands of every `AND` and `OR`, cheapest subtree first, so the evaluator's left-to-right short-circuit reaches a deciding cheap predicate before an expensive one it can then skip. A term reached through two paths inside one operand counts once toward that operand's cost (ADR-0002's per-evaluation term memoization). `COALESCE`, `IMPLIES` and `If` keep their operand order. The result is the same value, the same node count, and - without `OrderOptimizationOptions.MeasuredCosts` - the same order on every call. No migration step: these are new members, and a registry where no predicate sets `Cost` is unchanged by `OptimizeOrder()`. `docs/rewriting-rules.md` and `docs/predicates.md` cover it.
- `TextWriter` overloads for every printer. `CompiledRule.PrintRuleText`, `PrintJson`, `PrintPlainText`, `PrintMermaid`, `PrintEquation`, `PrintEquationWithLegend`, `PrintHtml` and `PrintDot` (and each `Decision` form), `PrintYaml` (in `TruthWeaver.Yaml`) and `RuleDiffPrinter.Print` each gain an overload with a `TextWriter writer` as the first parameter. The other parameters and defaults are the same. The overload writes exactly the text that the string form returns, does not flush or dispose the writer, throws `ArgumentNullException` for a null writer, and lets a failing writer's exception reach the caller unchanged. `PrintEquationWithLegend(writer)` writes the equation, a line feed and the legend text (only the equation when the rule has no term). No migration step: these are new members. `docs/examples.md` lists them.
- `RuleTextOptions` (`Width`, `IndentSize`, `Grouping`, `OperatorStyle`, `Constants`, with a static `Default`) and `CompiledRule.PrintRuleText(RuleTextOptions)` with its Try-first sibling `TryPrintRuleText(RuleTextOptions, out string?)`. `Width = null` (the default) gives the same one-line text as the other `PrintRuleText` overloads. A set `Width` breaks a group that does not fit across lines: an `AND`/`OR` chain or a binary connective breaks after each operator, one operand per line; a call form (`AtLeast(2, a, b, c)`, `BETWEEN(min, max, ...)`, `ANY(...)`) breaks one argument per line; nested groups indent by `IndentSize` (2 by default) per level. A single token longer than `Width` prints whole, on its own line, rather than being split. The output always compiles back to a tree equal to the original (proven by a property test over generated rules and widths). No migration step: the existing `PrintRuleText`/`TryPrintRuleText` overloads are unchanged. `docs/rule-text.md` covers it; the doc checker's `ruletext` marker takes a new `width=N` word.
- `CompiledRule.PrintHtml(HtmlOptions?)`, `CompiledRule.PrintDot(DotOptions?)`, a `Decision` overload of each, `HtmlOptions` and `DotOptions`. `PrintHtml` writes an HTML fragment: one infix formula in `span` elements with the classes `tw-rule`, `tw-operator`, `tw-term` and `tw-constant` (and `tw-true`, `tw-false`, `tw-unknown` or `tw-skipped` for a decision), named entities for the logic symbols and a `sub` element for the subscript of a simple variable. It has no `script` element and no `style` attribute, and one escape routine writes every term, argument and constant. `PrintDot` writes a Graphviz `digraph` with stable node ids (`n0`, `n1`) and a hexagon for an operator, a rounded box for a term and a circle for a constant, and the decision overload fills each node by state. `HtmlOptions` has `OperatorStyle`, `ShowArgumentValues`, `SimpleVariables` and `Constants`. `DotOptions` has `Direction` (a `MermaidDirection`), `NodeShapes`, `OperatorStyle`, `ShowArgumentValues` and `Constants`. Both have a static `Default`. No migration step: these are new members. The doc checker also knows the `html` and `dot` markers. `docs/html-and-dot.md` and `docs/examples.md` cover them.
- `EquationDialect.MathML`, `EquationDialect.Typst` and `EquationDialect.Ascii` for `PrintEquation` and `PrintEquationWithLegend`. `MathML` writes presentation MathML in one `math` element (operators as `mo`, text as `mtext`), and the output is well-formed XML for any rule. `Typst` writes Typst math markup between `$` signs, with each term as a quoted string. `Ascii` writes only the characters from space to tilde (`/\`, `\/`, `~`, `(+)`, `<->`, `->`), and it escapes each backslash and each non-ASCII character of a term. All three support constant styles, override strings, `SimpleVariables` and the legend. No migration step: the new values only add to the enum, but a `switch` over `EquationDialect` with no default arm needs three more arms. `docs/equations.md` and `docs/examples.md` cover them.
- `ConstantStyle` (`Word`, `Letters`, `LogicSymbols`) and `ConstantFormat` (a style plus `TrueText`, `FalseText` and `UnknownText` override strings that win over the style). They set how a printer writes `True`, `False` and `Unknown`: `EquationOptions.Constants` (Unicode, LaTeX, AsciiMath and CStyle; LaTeX writes `\top` and `\bot`, AsciiMath `top` and `bot`, and `Unknown` is `U` in a `\text` group or in quotes), `MermaidOptions.Constants` (with `MermaidOptionsBuilder.WithConstants`), the optional `constants` parameter of `PrintPlainText` and of `PlainTextTreePrinter.Print`, and `CompiledRule.PrintRuleText`. Rule text accepts only a format that the parser reads back (`Word`, or an override that is a letter-case variant of the constant's word): `Letters`, `LogicSymbols` and any other override throw `ArgumentException`, and the new `TryPrintRuleText(grouping, operatorStyle, constants, out text)` returns `false`. There is no `1`/`0`/`½` style. The default stays `Word`, so no output changes; `Letters` is the recommended style for strict-logic equations. `docs/equations.md`, `docs/rule-text.md`, `docs/rulebuilder.md` and `CONTEXT.md` cover it.
  **Breaking, binary only:** `CompiledRule.PrintRuleText`, `PrintPlainText` (both overloads) and `PlainTextTreePrinter.Print` (both overloads) gain a trailing optional `ConstantFormat? constants` parameter. Source that calls them compiles unchanged. Recompile any assembly that was built against an earlier build.
- `OperatorStyle` on `CompiledRule.PrintRuleText`: `Word` (the default, same output as before) or `Symbolic`, which
  writes the mathematical symbol the DSL parser itself reads back for `AND`/`OR`/`NOT`/`XOR`/`EQUIVALENT`/`IMPLIES`/
  `NAND`/`NOR`, so the result still compiles to an equal tree (proven by a property test over generated rules).
  `OperatorStyle.CStyle` is rejected with an `ArgumentException`, since the parser does not read `^` or `==`;
  `TryPrintRuleText` is the Try-first sibling that returns `false` instead. `EquationDialect.CStyle` is a new
  `PrintEquation` dialect for code-like output (`&&`, `||`, `!`, `^`, `==`); it renders `If` as the ternary
  `c ? t : f` instead of a call, a caveat documented on both types. `docs/rule-text.md` and `docs/equations.md`
  cover both.
- `Try` forms beside every method that returns a `CompilationResult<TContext>`: `RuleCompiler.TryCompile`, `TryCompileText`, `TryCompileJson` (text and `JsonElement`), `TryCompileYaml` (text and `YamlNode`), `RuleBuilder.TryCompile`, and on `CompiledRule` `TrySimplify`, `TryCanonicalize`, `TryCompressToDerived`, `TryExpandToPrimitives`, `TryExpandToNand`, `TryExpandToNor`, `TryToNnf`, `TryToCnf` and `TryToDnf`. Each is `bool Try…(…, out CompiledRule<TContext>? rule, out EquatableArray<Diagnostic> diagnostics)`; `rule` is non-null exactly when the method returns `true`, and a warning-only result returns `true`. `CompilationResult.TryGetRule` is the sibling of `GetRuleOrThrow`. Nothing breaks; the existing methods are unchanged. The convention (every public `…OrThrow` has a `Try…` sibling, and documentation shows the `Try` form first) is in `docs/contributing.md` and is enforced by an architecture test.
- MinVer derives the package version from Git tags with the prefix `v`. An untagged build is `1.0.0-preview.0.N`. The
  hand-set `Version` of `1.0.0-dev` is removed from `Directory.Build.props`. CI checks out the full history so the
  version is correct.
- `EnablePackageValidation` for the packable projects, a `pack` job in CI (`dotnet pack -warnaserror` and `scripts/check-packages.ps1`, which never pushes), and `docs/releasing.md`, the release runbook. SourceLink is verified on a local pack. The `Microsoft.SourceLink.GitHub` package reference is removed because it switched off the SDK Source Link, so the packed assemblies had no source link; the .NET SDK Source Link now supplies it.
- Each package sets its own `PackageId`, `Description` and `PackageTags`, and `PackageReleaseNotes` points to this file.
- `docs/contributing.md`: what the pre-commit hook and `scripts/format-all.ps1` do, linked from the README.
- `FakePredicates` factories label a predicate with its name when no `label` is given, instead of the constant `"Fake"`, so
  rendered diagrams show a distinct label for each fake. `PredicateHarness` and `docs/predicate-harness.md` state that the
  harness calls the predicate twice for each argument set and once for the cancellation check.
- `MermaidOptions.Default` and `RuleFuzzerOptions.Default`, as `EquationOptions.Default` and `PredicateHarnessOptions.Default`
  already exist. The optional-parameter `CompiledRule.PrintMermaid` overloads document that they fix `OperatorStyle.Word`.
- Reversed literal bounds on `Between` and `NotBetween` (`NumericPredicates` for `Int64` and `Decimal`, and
  `DateTimePredicates`) are now a compile-time error, the new `DiagnosticCodes.InvalidArgumentValue` (`TRE0026`), at the
  predicate call in rule text, JSON, YAML and `RuleBuilder` rules, with a suggestion to swap the bounds. `Compile` returns
  no rule. Equal bounds still compile. A bound that is not a literal, such as a value read from a data source, still
  throws `ArgumentException` at evaluation (`Unknown` plus a `Fault`). The bounds are never swapped.
- `PredicateSchema.ArgumentValidator` and `PredicateArgumentProblem`: an optional check over the literal argument values
  of a predicate call. Each problem it returns is a `TRE0026` error. It is `null` by default, so existing schemas are
  unchanged.
- `RuleCompiler<TContext>.CompileText(string)`: the same method as `Compile(string)`, which stays. It names the DSL
  the way `CompileJson` and `CompileYaml` name their own formats (least-surprise-normalization 24).

- `NotEqualsIgnoreCase`, `NotStartsWith`, `NotEndsWith` and `NotEqualsConfigurable` in `StringPredicates` and `NotSetEquals`
  in `CollectionPredicates`: the Strong Kleene complements of their positive members.

- `Arg.TryFrom` (returns the validator's `QueryProblem` list instead of throwing `ArgumentException`) and
  `IDataSource.TryGetAsync<T>` in `TruthWeaver.Building`, which returns a `DataReadResult<T>` (`Succeeded`, `Value`,
  `FailureKind`, `ErrorMessage`) instead of throwing `InvalidOperationException`. `Arg.From` and `GetAsync<T>` still throw.
- `TypePredicates`: the type tests `IsGuid`, `IsNumeric`, `IsUrl`, `IsString` and `IsDateTimeOffset` and their `IsNot...` twins
  (the Strong Kleene complement). Each has a `string?` (parse-based) and an `object?` (runtime-type) overload; a null
  selected value is `Unknown`.
- `Try` forms for reading optional or mixed-kind arguments without exception handling: `LiteralValue.TryAsString`, `TryAsInt64`,
  `TryAsDecimal`, `TryAsBoolean`, `TryAsDateTimeOffset`, `TryAsGuid` and `TryAsArray`, and `PredicateArguments.TryGetString`,
  `TryGetInt64`, `TryGetDecimal`, `TryGetBool`, `TryGetDateTimeOffset`, `TryGetGuid`, the matching `TryGet…Array` forms and
  `TryGetRaw`. Each returns `false` for a missing name or a different kind. The existing `As…` and `Get…` members still throw.
- Collection and date/time predicates in `TruthWeaver.Predicates`: `CollectionPredicates` adds `IsEmpty`, `Contains`, `ContainsAny`, `ContainsAll`, `IsSubsetOf`, scalar `In` and the `Count*` comparisons, and the new `DateTimePredicates` adds `After`, `Before` and `Between` over `DateTimeOffset`. Each has a `NotX` Strong Kleene twin (`Outside` for `Between`). Null selections return `Unknown` by default, with `NullBehavior.False` as the host option, and reversed `Between` bounds throw `ArgumentException`. `DateTimePredicates` also adds the clock predicates `AfterNow` and `BeforeNow` with twins `NotAfterNow` and `NotBeforeNow`; each factory requires a `TimeProvider` and there is no ambient clock.
- Strong Kleene (K3) language surface (see [ADR-0005](docs/adr/0005-strong-k3-language-surface.md)): `Unknown` as a first
  class value and constant; the operators `IMPLIES`, `EQUIVALENT` (`IFF`), `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`,
  `BETWEEN`, `COALESCE`, `If` (`? :`) and the inspections `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`; symbol and Unicode
  input aliases; `[]` and `{}` grouping; depth-cycling delimiters.
- Rewrites on `CompiledRule`: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`
  and `Simplify`.
- `Decision.Collapse(CollapsePolicy)` and `Decision.Project(...)` to resolve `Unknown` at the call site.
- Dual-rail K3 analyzer, structured diagnostics with suggestions and spans (JSON diagnostics carry paths and source
  spans), `DiagnosticFormatter`.
- `RuleEquivalence.Compare` (equivalent, not equivalent with a counter-example, or undecided) and
  `RuleDiffResult.PreservesMeaning`.
- Opt-in lint rules through `CompilerOptions.Lints` (`TRE0017` to `TRE0023`, `TRE0028` to `TRE0030`), `Diagnostic.EnclosedBy`, and the `CompilerOptions` values `DeepNestingFraction` and `WideChainOperandLimit`.
- Rewrite tooling: `CompiledRule.ToNnf`, `ToCnf` and `ToDnf` with `NormalFormOptions` (diagnostic `TRE0031` when a
  threshold stays an atom), `CompiledRule.SimplifyWithSteps` (`RewriteStep`, `RewriteLaw`), and
  `RewriteAssertions.AssertSound` with `RewriteExpectations` in `TruthWeaver.Testing`.
- `CompilerOptions.MaxRewriteNodeCount` (default 100,000) and diagnostic `TRE0016` for oversized expansions.
- Predicate catalog additions: the string members `IsEmpty`, `IsNotEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`,
  `IsNotNullOrWhiteSpace`, `NotEqual`, `NotContains` and `RegexPredicates.NotMatches`; `NumericPredicates` (`Int64` and
  `Decimal` equality, ordering, `Between`/`Outside`, `In`/`NotIn`, null and default tests); and `ScalarPredicates`
  (`Boolean`, `Guid` and `DateTimeOffset` equality, `In`/`NotIn`, null and default tests). Every positive member has a
  `NotX` twin that is the Strong Kleene complement.
- `NullBehavior` option on the built-in string, regex and collection predicates; `IEnumerable<RuleBuilder>` overloads
  for the counted operators.
- Benchmarks for the new operators, rewrites and diagnostics formatting.
- `RuleDiff.Compare` takes an optional `CompilerOptions`, passed to the equivalence check, so a raised `MaxAnalysisTerms` decides `PreservesMeaning` for large rules.
- Data sources for expression variables (see [ADR-0006](docs/adr/0006-data-sources-for-expression-variables.md) and the
  [guide](docs/data-sources.md)), DSL part: a term argument can be `from("source", "query")`, resolved on every evaluation from
  a named `IDataSource` passed to the new `CompiledRule.EvaluateAsync(context, services, dataSources, options, cancellationToken)`
  overload. New in `TruthWeaver.Abstractions`: `IDataSource`, `DataQueryResult`, `DataQueryErrorKind`, `DataSources`,
  `VariableReference`, `VariableFailureKind` and `VariableResolutionException` (the `Fault.Exception` for a failed lookup), and
  `TermIdentity.Variables` (references are part of a term's identity by source name and query text). Source names are declared
  with `CompilerOptions.DataSources` (`DataSourceDeclarations`); an undeclared name is `TRE0024` with a "did you mean". A scalar
  argument needs exactly one match, an array argument collects all matches (none gives an empty array), conversions are those of
  DSL literals and no wider, and each `(source, query)` pair is queried once per evaluation (a failure is replayed, not retried). A source that errors,
  throws or times out by itself gives `Unknown` plus a `Fault`; cancelling the evaluation cancels it, as for predicates. `TruthWeaver.Testing` adds
  `FakeDataSource`. JSON/YAML input of variables, query validators and `RuleBuilder` support follow.
- Executable data-source documentation (data-sources 09): the rule, JSON and YAML examples in `docs/data-sources.md` and the README are checked by `DocExampleTests` (the checker now declares the `user` and `request` sources with `JsonQueryValidator`, and knows `ageAtLeast` and `hasRole`), and the guide's C# snippets run in `DataSourcesGuideTests`. The new `TruthWeaver.DataSources.Json` package is listed with the others.
- `RuleBuilder` support for data sources (data-sources 08): `Arg.From(source, query, validator = null)` (in `TruthWeaver.Building`) returns a `VariableReference` that
  `RuleBuilder.Predicate` renders as the `{ "from", "query" }` argument form, so it compiles to the same canonical text as the DSL (an optional
  `IQueryValidator` rejects a malformed query with `ArgumentException` immediately). `IDataSource.GetAsync<T>(query, cancellationToken)` reads a value at
  build time for use as an ordinary literal. `RuleBuilder` argument values now also accept `Guid`.
- Trace redaction (data-sources 07): `EvaluationOptions.IncludeResolvedValues` (default `false`) adds the resolved value after each variable
  reference in the trace text (`min: from("user", "$.minAge") = 18`). By default the trace names only the reference, and fault messages never contain values.
- `YamlDataSource` in `TruthWeaver.Yaml` (data-sources 06): `Parse(yaml)` and `Create(YamlNode)` read a YAML document into the JSON data
  model (quoted scalars are strings, plain scalars follow the core schema limited to JSON's types, an alias is a copy of its
  anchor) and answer queries with the JSON package's JSONPath engine, so one query gives the same result against equivalent JSON
  and YAML and `JsonQueryValidator` serves YAML sources too. `TruthWeaver.Yaml` now references `TruthWeaver.DataSources.Json`.
- Compile-time query validation (data-sources 05): `IQueryValidator` and `QueryProblem` in `TruthWeaver.Abstractions`; a source name
  can be declared with a validator (`DataSourceDeclarations.Add(name, validator)` or `declarations[name] = validator`), and a
  malformed query is then a `TRE0025` error (`DiagnosticCodes.MalformedDataQuery`) at the query string, in the DSL, JSON and YAML.
  `TruthWeaver.DataSources.Json` ships `JsonQueryValidator.Instance`, which parses RFC 9535 JSONPath without a document.
- New package `TruthWeaver.DataSources.Json` (data-sources 04): `JsonDataSource` (`Parse(json)`, `Create(JsonNode?)`) answers JSONPath
  (RFC 9535) queries over a JSON document and converts each matched node to a `LiteralValue` (string, `long`, `decimal`, boolean;
  an object, array or `null` is `DataQueryErrorKind.UnsupportedType`). `ScopeAsync` roots a new source at the single node a query
  matches. It depends on `TruthWeaver.Abstractions` and Meziantou.Framework.JsonPath 3.0.7 (MIT, no further dependencies; it replaced
  JsonPath.Net, whose 3.x binaries ship under the Open Source Maintenance Fee EULA), and the core `TruthWeaver` package gains no third-party dependency.
- Variable references in JSON and YAML (data-sources 03): an argument value of `{ "from": "user", "query": "$.minAge" }` (the same
  mapping in YAML) is a variable reference and compiles to the same tree as `from("user", "$.minAge")`; `rule-tree.schema.json`
  accepts it (a reference is not allowed inside an array literal). A malformed reference is a `TRE0014` diagnostic at the wrong
  member, and an undeclared source name (`TRE0024`) points at the `from` member.

- `JsonDataSource.TryParse(text, out source, out error)` and `YamlDataSource.TryParse(text, out source, out error)` for host code that
  reads untrusted or user-edited documents. They return `false` for malformed JSON or YAML, a duplicate YAML key and a
  self-referential alias. The error text gives the position and never repeats document content. `Parse` still throws.
- `DataScopeResult` (in `TruthWeaver.Abstractions`) and the `DataQueryErrorKind` values `NoMatch` and `AmbiguousMatch`; see the
  breaking change to `IDataSource.ScopeAsync` below. `FakeDataSource.FailingScope(query, message, kind)` scripts a failed scope.

### Changed

- `Fault` equality compares the term, the exception type name (the new `Fault.ExceptionType`) and the message, and for two `VariableResolutionException`s also the reference and failure kind, instead of the exception object. The same failure in two evaluations now gives equal faults and equal decisions, and a fault read back from a `DecisionRecord` equals the fault that was written. Migration: code that relied on two faults with different exception objects being unequal compares `fault.Exception` by reference itself. `docs/decision-records.md` covers it.
- **Breaking:** the rule text parser reads the bare letters `T`, `F` and `U` (any case) as the constants `True`, `False` and
  `Unknown`, so `PrintRuleText` and `TryPrintRuleText` now accept `ConstantStyle.Letters` (and an override that is a letter of
  the constant's own value, for example `TrueText = "t"`) and the output compiles back to an equal tree. `LogicSymbols` is
  still rejected. `T`, `F` and `U` become reserved names, in any case: registering a predicate with one of these names throws
  `ArgumentException`, the source generator reports `TWG007`, and a term with one of these names (from JSON, YAML or
  `RuleBuilder`) is an `UnknownPredicate` error whose message says the name is reserved. Migration: rename a predicate named
  `T`, `F` or `U` (any case) before you upgrade, and rename a rule-text term `t`, `f` or `u` to a longer name. JSON and YAML
  still read the words only. [Rule text](docs/rule-text.md#constant-style) describes it.
- **Breaking:** `ConstantStyle.Letters` (`T`, `F`, `U`) is the default constant style of `PrintEquation` in every dialect
  (Unicode, LaTeX, AsciiMath, CStyle, MathML, Typst and Ascii) and of `PrintEquationWithLegend`. `EquationOptions.Default.Constants`
  is now a `Letters` format, not `ConstantFormat.Default`. `PrintRuleText`, `PrintPlainText`, `PrintMermaid`, `PrintHtml` and
  `PrintDot` keep `Word`. Migration: to get `True`, `False` and `Unknown` back, pass
  `Constants = new ConstantFormat { Style = ConstantStyle.Word }` in the `EquationOptions`. [Equations](docs/equations.md#constants)
  describes it. The doc-example checker takes a new `words` word, and an equation or legend block with no constant word documents
  the letters default.
- **Breaking:** predicate factories follow one twin-naming guide ([Predicate conventions](docs/predicate-conventions.md#naming)).
  A state or relation is `IsX` with the twin `IsNotX`. A verb phrase is `Verb` with the twin `NotVerb`. A twin is always the
  Strong Kleene complement of its positive. `Equals`, `Equal`, `NotEquals` and `NotEqual` become `IsEqualTo` and `IsNotEqualTo`.
  `EqualsConfigurable` merges into the string `IsEqualTo`, so one predicate takes the optional rule arguments `ignoreCase` and
  `trim` (both `false` by default). `EqualsIgnoreCase` and `NotEqualsIgnoreCase` are removed: they are `ignoreCase: true`.
  The four ordering predicates get exact-complement twins. There are no `[Obsolete]` forwarders, because the packages are
  unpublished. The reference pages under `docs/strong-k3/predicates` are renamed to match.

  | Old | New |
  | --- | --- |
  | `StringPredicates.Equals` | `StringPredicates.IsEqualTo` |
  | `StringPredicates.NotEquals` | `StringPredicates.IsNotEqualTo` |
  | `StringPredicates.EqualsIgnoreCase` | `StringPredicates.IsEqualTo`, with `ignoreCase: true` in the rule |
  | `StringPredicates.NotEqualsIgnoreCase` | `StringPredicates.IsNotEqualTo`, with `ignoreCase: true` in the rule |
  | `StringPredicates.EqualsConfigurable` | `StringPredicates.IsEqualTo` |
  | `StringPredicates.NotEqualsConfigurable` | `StringPredicates.IsNotEqualTo` |
  | `StringPredicates.In`, `NotIn` | `StringPredicates.IsIn`, `IsNotIn` |
  | `NumericPredicates.Equal`, `NotEqual` | `NumericPredicates.IsEqualTo`, `IsNotEqualTo` |
  | `NumericPredicates.LessThan` | `NumericPredicates.IsLessThan`, new twin `IsNotLessThan` |
  | `NumericPredicates.GreaterThan` | `NumericPredicates.IsGreaterThan`, new twin `IsNotGreaterThan` |
  | `NumericPredicates.LessThanOrEqual` | `NumericPredicates.IsLessThanOrEqualTo`, new twin `IsNotLessThanOrEqualTo` |
  | `NumericPredicates.GreaterThanOrEqual` | `NumericPredicates.IsGreaterThanOrEqualTo`, new twin `IsNotGreaterThanOrEqualTo` |
  | `NumericPredicates.Between`, `NotBetween` | `NumericPredicates.IsBetween`, `IsNotBetween` |
  | `NumericPredicates.In`, `NotIn` | `NumericPredicates.IsIn`, `IsNotIn` |
  | `ScalarPredicates.Equal`, `NotEqual` | `ScalarPredicates.IsEqualTo`, `IsNotEqualTo` |
  | `ScalarPredicates.In`, `NotIn` | `ScalarPredicates.IsIn`, `IsNotIn` |
  | `DateTimePredicates.After`, `NotAfter` | `DateTimePredicates.IsAfter`, `IsNotAfter` |
  | `DateTimePredicates.Before`, `NotBefore` | `DateTimePredicates.IsBefore`, `IsNotBefore` |
  | `DateTimePredicates.AfterNow`, `NotAfterNow` | `DateTimePredicates.IsAfterNow`, `IsNotAfterNow` |
  | `DateTimePredicates.BeforeNow`, `NotBeforeNow` | `DateTimePredicates.IsBeforeNow`, `IsNotBeforeNow` |
  | `DateTimePredicates.Between`, `NotBetween` | `DateTimePredicates.IsBetween`, `IsNotBetween` |
  | `DateTimePredicates.OnDayOfWeek`, `NotOnDayOfWeek` | `DateTimePredicates.IsOnDayOfWeek`, `IsNotOnDayOfWeek` |
  | `DateTimePredicates.InMonth`, `NotInMonth` | `DateTimePredicates.IsInMonth`, `IsNotInMonth` |
  | `DateTimePredicates.InTimeWindow`, `NotInTimeWindow` | `DateTimePredicates.IsInTimeWindow`, `IsNotInTimeWindow` |

  **Migrate:** rename each factory call as the table shows. The name that a host gives a predicate at registration does not
  change, so rule text is unaffected, except where a rule used a registration of `EqualsIgnoreCase` or `NotEqualsIgnoreCase`:
  register `IsEqualTo` or `IsNotEqualTo` and add `ignoreCase: true` to those rules. The default labels follow the new names
  (for example `Is Less Than`).

- **Breaking:** the regular-expression predicates become `IsMatch` and `IsNotMatch`, and take the optional rule argument
  `flags`: a string of the single letters `i` (`RegexOptions.IgnoreCase` with `RegexOptions.CultureInvariant`), `m`
  (`Multiline`), `s` (`Singleline`), `n` (`ExplicitCapture`) and `x` (`IgnorePatternWhitespace`). The separate
  case-insensitive predicates are removed: they are `flags: "i"`. An unknown flag letter, a repeated flag letter and an
  invalid pattern, when literal, are `TRE0026` compile errors. The pattern cache is keyed on the pattern and the options. There
  are no `[Obsolete]` forwarders, because the packages are unpublished.

  | Old | New |
  | --- | --- |
  | `RegexPredicates.Matches` | `RegexPredicates.IsMatch` |
  | `RegexPredicates.NotMatches` | `RegexPredicates.IsNotMatch` |
  | `RegexPredicates.MatchesIgnoreCase` | `RegexPredicates.IsMatch`, with `flags: "i"` in the rule |
  | `RegexPredicates.NotMatchesIgnoreCase` | `RegexPredicates.IsNotMatch`, with `flags: "i"` in the rule |

  **Migrate:** rename each factory call as the table shows. For a registration of `MatchesIgnoreCase` or
  `NotMatchesIgnoreCase`, register `IsMatch` or `IsNotMatch` and add `flags: "i"` to those rules. The default labels are now
  `Is Match` and `Is Not Match`.

- **Breaking:** the collection count predicates are renamed to `HasCount…` verb phrases, and their twins to
  `NotHasCount…` (Form B in the [naming guide](docs/predicate-conventions.md#naming)). Behavior, arguments and null handling
  do not change. The default labels follow the new names (for example `Has Count Equal To`). There are no `[Obsolete]`
  forwarders, because the packages are unpublished. The reference pages under `docs/strong-k3/predicates` are renamed to
  match, and their family is corrected to `CollectionPredicates`.

  | Old | New |
  | --- | --- |
  | `CollectionPredicates.CountEqual`, `NotCountEqual` | `CollectionPredicates.HasCountEqualTo`, `NotHasCountEqualTo` |
  | `CollectionPredicates.CountGreaterThan`, `NotCountGreaterThan` | `CollectionPredicates.HasCountGreaterThan`, `NotHasCountGreaterThan` |
  | `CollectionPredicates.CountGreaterThanOrEqual`, `NotCountGreaterThanOrEqual` | `CollectionPredicates.HasCountGreaterThanOrEqualTo`, `NotHasCountGreaterThanOrEqualTo` |
  | `CollectionPredicates.CountLessThan`, `NotCountLessThan` | `CollectionPredicates.HasCountLessThan`, `NotHasCountLessThan` |
  | `CollectionPredicates.CountLessThanOrEqual`, `NotCountLessThanOrEqual` | `CollectionPredicates.HasCountLessThanOrEqualTo`, `NotHasCountLessThanOrEqualTo` |

  **Migrate:** rename each factory call as the table shows. The name that a host gives a predicate at registration does not
  change, so rule text is unaffected.

- **Breaking (behavior):** `IsLessThanOrEqualTo` and `IsGreaterThanOrEqualTo` (formerly `LessThanOrEqual` and
  `GreaterThanOrEqual`) are positive predicates. Registered with `NullBehavior.False`, a null selected value now answers
  `False`, like every other positive predicate. Before, it answered `True`, because they were built as the twins of
  `GreaterThan` and `LessThan`. **Migrate:** to keep the old answer for a null value, register `IsNotLessThan` (for
  `IsGreaterThanOrEqualTo`) or `IsNotGreaterThan` (for `IsLessThanOrEqualTo`) with `NullBehavior.False`.
- The string `IsEqualTo` and `IsNotEqualTo` read a missing `ignoreCase` or `trim` argument as `false`, so a delegate called
  directly with only the comparison target behaves as the rule-text default.
- **Breaking:** `RuleMetrics.MaxDepth` is renamed `Depth`, so a measured value no longer shares a name with the
  `CompilerOptions.MaxDepth` limit (least-surprise-normalization 29). `NodeCount` keeps its name; its limit stays
  `CompilerOptions.MaxNodeCount`. Migration: read `rule.Metrics.Depth` instead of `rule.Metrics.MaxDepth`.
- **Breaking:** `CompiledRule.CompressToDerived()`, `Canonicalize()` and `Simplify()` return a
  `CompilationResult<TContext>` instead of a `CompiledRule<TContext>`, and `SimplifyResult<TContext>` has a third member,
  `Diagnostics`. Each rewrite repeats until the rule stops changing, with a pass limit of 16, 8 and 16. A rewrite that
  reaches the limit now returns the new warning `RewriteNotConverged` (`TRE0033`) with an equivalent rule, instead of
  stopping silently; rewriting that rule again can still change it. No real input reaches a limit today. Migration:
  call `.GetRuleOrThrow()` (or read `.Rule` after checking `Succeeded`) on the result; construct a `SimplifyResult`
  with `EquatableArray<Diagnostic>.Empty` as the third argument. `RewriteAssertions.AssertSound(rule, r => r.Simplify())`
  compiles unchanged and now uses the `CompilationResult` overload.
- **Breaking:** the list-valued members of `Decision` (`Faults`), `Trace` (`Entries`), `TraceNode` (`Children`) and
  `CompilationResult<TContext>` (`Diagnostics`) are `EquatableArray<T>` instead of `IReadOnlyList<T>`
  (least-surprise-normalization 28), so two decisions from identical evaluations, two identical compiles, and two
  equal traces now compare equal and share a hash code, instead of the reference equality a plain list gave two
  separately-allocated results. `EquatableArray<T>` still implements `IReadOnlyList<T>`, so reading a member (`.Count`,
  indexing, `foreach`, LINQ) is unaffected. Migration: a caller that passes a list literal or an existing
  `List<T>`/array positionally to one of these constructors, or in a `with` expression, wraps it in
  `new EquatableArray<T>(...)`; an empty collection expression (`[]`) still compiles unchanged.
- The `end` and `duration` arguments of `DateTimePredicates.InTimeWindow` and `NotInTimeWindow` are optional with no
  default, and an omitted one means "not given". They defaulted to empty text. **Breaking:** `end: ""` and
  `duration: ""` are now `TRE0026` compile errors (an empty text is not a time of day or a duration), and a schema or
  `PredicateArguments` built by hand no longer carries the empty default. Migration: omit the argument instead of passing
  `""`. `PredicateArguments` takes an optional `deferred` set and has `IsDeferred(name)`, and the compiler passes the names of
  the data-source arguments to `PredicateSchema.ArgumentValidator`, so a validator can tell an omitted argument from one that
  a data source supplies.
- `RegexPredicates.MatchesIgnoreCase` and `NotMatchesIgnoreCase` are new: `Matches` and `NotMatches` with
  `RegexOptions.IgnoreCase | RegexOptions.CultureInvariant`. The pattern cache of `RegexPredicates` is now bounded: each
  cache (case-sensitive and case-insensitive) holds 256 patterns and clears itself when the next new pattern would
  exceed that. Compiled patterns are no longer kept for the life of the process. **Breaking:** a literal `pattern`
  argument of `Matches`, `NotMatches`, `MatchesIgnoreCase` or `NotMatchesIgnoreCase` that is not a valid regular
  expression is now a `TRE0026` compile error at the call (rule text, JSON, YAML and `RuleBuilder`), and `Compile`
  returns no rule. It was an evaluation-time fault (`Unknown` plus a `Fault`). A pattern that a data source supplies is
  still checked at evaluation. Migration: fix the pattern, or move it to a data source if the fault is intended.
  A pattern that began with `(?i)` can use `MatchesIgnoreCase` instead.
- `RuleBuilder.Compile` now compiles its rendered tree through `RuleCompiler.CompileJson(JsonElement)` instead of
  `CompileJson(string)` (least-surprise-normalization 24). A diagnostic located in the tree still carries the same
  `Path` as before (for example `$.operands[1].predicate`), but its `Span` is now always `SourceSpan.None` instead
  of a span into the JSON text `RuleBuilder` generated internally and never showed to the caller; that span never
  located anything in the caller's own builder tree. Migration: read the diagnostic's `Path`, not its `Span`, for a
  `RuleBuilder` diagnostic. A caller that only checked `Succeeded` or `Diagnostics` is unaffected.
- `TruthWeaverAssertionException` is a new abstract base in `TruthWeaver.Testing`. `DecisionAssertionException`,
  `PredicateHarnessException` and `RuleFuzzException` derive from it, so existing catches keep working.
  **Breaking:** `DecisionAssertions.HaveFaultForTerm(predicateName)` is renamed `HaveFaultForPredicate(predicateName)`, to
  match its argument. Migration: rename the call.
- **Breaking:** `Decision.Project(bool unknownAs)` is now `Decision.Project(TruthValue unknownAs)`, so a call reads
  `d.Project(TruthValue.True)` instead of `d.Project(true)`. Only `True` and `False` are valid. `Unknown` or an undefined
  value throws `ArgumentOutOfRangeException` for every result, the same up-front rule `Decision.Collapse` applies to an
  undefined policy. Migration: replace `Project(true)` with `Project(TruthValue.True)` and `Project(false)` with
  `Project(TruthValue.False)`.
- `RewriteAssertions.AssertSound` gains an overload for a rewrite that returns a `CompilationResult<TContext>`, so
  `r => r.ToCnf()` and the other capped rewrites need no `.CompiledRule!`. A result with no rule or an error
  diagnostic fails the assertion with a `'compiles'` check that shows the diagnostics. A call that passes a bare `null`
  as the rewrite is now ambiguous: cast it to the delegate type. `docs/diagnostics.md` holds one table of the compile
  and evaluation limits, and `docs/rewriting-rules.md` states what each rewrite returns.
- The XML documentation of the expanding rewrites and the normal forms, and `docs/rewriting-rules.md`, state that a
  rewrite result over `CompilerOptions.MaxNodeCount` (512 by default) compiles back from its text only when
  `MaxNodeCount` is raised. The two caps stay independent. No behavior changed.
- Breaking: `NullBehavior.Unknown` is the zero value (`0`) and `NullBehavior.False` is `1`. They were `1` and `0`, so
  `default(NullBehavior)` was `False` while the documented default is `Unknown`. A forgotten or defaulted value now means
  `Unknown`. Migration: nothing changes when you name the members. Replace any stored or cast numeric value (a
  `(NullBehavior)0` that meant `False` is now `Unknown`) with the member name, and re-read a persisted `NullBehavior` as
  its name rather than its number.
- Breaking: the collection `IsEmpty` and `IsNotEmpty` take a `NullBehavior` that defaults to `Unknown`, as the string
  `IsEmpty` and the count predicates do. A null collection answered `True` for `IsEmpty` and `False` for `IsNotEmpty`
  before; it is now `Unknown` for both, with no fault. A missing value is `Unknown` unless the predicate is a null test.
  Migration: register the predicates with `nullBehavior: NullBehavior.False` to read a null collection as "not empty"
  (`IsEmpty` `False`, `IsNotEmpty` `True`). To treat a null collection as empty, have the selector return an empty
  collection (`c => c.Tags ?? []`).
- Breaking: `EvaluationOptions.FaultBudget = N` aborts evaluation when the Nth fault is recorded, as ADR-0002 and the
  XML docs state. It aborted on the (N+1)th before, so `FaultBudget: 1` tolerated one fault. A budget below 1 now makes
  `CompiledRule.EvaluateAsync` throw `ArgumentOutOfRangeException`; `0` used to abort on the first fault. `null` is still
  unlimited. The `Timeout` docs now say that an expired timeout throws `OperationCanceledException` and is not a fault.
  Migration: add 1 to a budget you set to tolerate N faults (`FaultBudget: 1` becomes `2`), and replace `0` with `1`.
- Breaking: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `ToNnf`, `ToCnf` and `ToDnf` cap their result at the
  rule's own `CompilerOptions.MaxRewriteNodeCount`, so a rule compiled with a raised cap expands past 100,000 nodes with
  no argument. Their `CompilerOptions? options` parameter is now `int? maxNodeCount`, the cap for that one call.
  `RuleEquivalence.Compare`, `RuleDiff.Compare`, `RuleAssertions.AssertEquivalent` and `RewriteAssertions.AssertSound`
  take `int? maxAnalysisTerms` instead of `CompilerOptions? options`; they read nothing else. Migration: replace
  `rule.ExpandToNand(new CompilerOptions(MaxRewriteNodeCount: n))` with `rule.ExpandToNand(n)` or compile the rule with
  that option, and replace `new CompilerOptions(MaxAnalysisTerms: n)` with `n`.
- Breaking: the `RuleBuilder` array (`params`) and sequence (`IEnumerable<RuleBuilder>`) overloads of an operator give
  the same rule. `And`, `Or`, `Any`, `All` and `None` fold in both: an empty list is the identity constant (`None` of
  one operand is its negation, the others are the operand itself). `Parity`, `ExactlyOne` and `Coalesce` no longer fold
  a short sequence: they build the node and the compiler reports `MalformedTree`, as the array form does. `GreaterThan`
  and `LessThan` gain the `IEnumerable<RuleBuilder>` overload. An argument value of an unsupported type, including
  `null`, is an `ArgumentTypeMismatch` diagnostic from `Compile` and no longer an `ArgumentException`. Migration: check
  the count before you build a `Parity`, `ExactlyOne` or `Coalesce` from a list that can have fewer than two items, and
  expect a one-operand array under `And`, `Or`, `Any`, `All` or `None` to fold to the operand.
- Breaking: a JSON or YAML rule node that has a key its kind does not define is a `MalformedTree` (`TRE0014`) compile
  error at that key, as `rule-tree.schema.json` already states. This covers an unknown key, a misspelt `args`, a
  `predicate` next to an `op`, and a stray `k`, `min` or `max` on an operator that takes none. The key was ignored
  before. Migration: delete the key, or correct its spelling (the diagnostic suggests the nearest valid key).
- Breaking: a predicate argument that is named twice is the new error `DuplicateArgument` (`TRE0032`), in rule text,
  JSON, YAML and `RuleBuilder`. The last value won before. `RuleBuilder.ToJson()` throws `InvalidOperationException`
  for a duplicate, because JSON cannot hold both; `Compile` returns the diagnostic. Migration: keep one occurrence of
  each argument.
- Breaking: a date-time literal must end in `Z` or carry an offset such as `+02:00`. Text with no offset, such as
  `"2026-01-01"` or `"2026-01-01T09:00"`, is an `ArgumentTypeMismatch` compile error that names the fix, in the DSL,
  JSON, YAML and `RuleBuilder`. The same rule applies to a string a data source resolves for a date-time argument
  (the variable faults to `Unknown`). It was read in the host time zone, so one stored rule could mean different
  instants on different hosts. Migration: append `Z` (UTC) or the offset you meant to each date-time literal and each
  stored date-time value.
- Breaking: in the rule text, a threshold `k` that is not a whole number in the `int` range, such as `AtLeast(1.5, a, b)`
  or `AtLeast(99999999999, a, b)`, is an `InvalidThresholdValue` (`TRE0008`), the same as a bad `BETWEEN` bound. It used to
  compile as `k = 0`. `k`, `min` and `max` share one integer parser that uses the invariant culture. Migration: write `k` as an
  integer literal.
- Breaking: one authoring mistake has one diagnostic code in rule text, JSON, YAML and `RuleBuilder`. A wrong operand
  count for any operator is `InfixArityViolation` (`TRE0006`); it was `MalformedTree` (`TRE0014`) for every operator
  except the binary ones. A threshold `k` or `BETWEEN` bound that is missing or not a whole number is
  `InvalidThresholdValue` (`TRE0008`); it was `SyntaxError` (`TRE0001`) in rule text and `MalformedTree` in JSON and
  YAML. A JSON or YAML `op` that does not exist, and a declared `Collapse`, `Project` or `NXOR` in any format, is
  `UnknownPredicate` (`TRE0002`); they were `TRE0001` in rule text and `TRE0014` in JSON and YAML. A repeated argument in
  a YAML `args` mapping is `DuplicateArgument` (`TRE0032`) like the other formats; it was a `TRE0014` YAML syntax error.
  `MalformedTree` (`TRE0014`) is now only for the shape of a JSON or YAML tree. No code was renumbered. Migration: match
  on the new code where you matched on the old one. The code table in `docs/strong-k3/specification/diagnostics.md` lists
  every code with its severity, phase and default, and a test fails when a code in `DiagnosticCodes` is missing from it.
- Breaking: a predicate argument name matches its schema argument ignoring case, as predicate and operator names
  already do. `hasCrust(Crust: "thin")` now compiles to the term `hasCrust(crust: "thin")`, in rule text, JSON, YAML and
  `RuleBuilder`; the canonical text keeps the schema's spelling. It was an `UnknownArgument` (`TRE0005`) error before.
  The same argument written twice in two cases is `DuplicateArgument` (`TRE0032`). A schema that declares two
  argument names that differ only in case now fails at registration with an `ArgumentException`. Migration: rename one of
  the two arguments in such a schema, and remove any rule text that relied on `Crust:` being a different name from
  `crust:`. Argument values stay case-sensitive.
- Breaking: `LintRules.All` no longer includes the not-canonical lint (`TRE0030`), so a first run reports likely mistakes
  only. The lint moved to a new `LintRules.Style` flag, which replaces `LintRules.NotCanonical`. Migration: write
  `LintRules.All | LintRules.Style` where you relied on `All` to report `NotCanonical`, and rename any use of
  `LintRules.NotCanonical` to `LintRules.Style`. The diagnostic code and the `DiagnosticCodes.NotCanonical` constant are
  unchanged.
- Breaking: an unquoted `null`, `Null`, `NULL`, `~` or empty value as a predicate argument in YAML is rejected like JSON `null`: the compile
  diagnostic says the argument is not a string, number, boolean or array, at the argument's path. It was read as the string
  "null" (or an empty string). Migration: quote the value (`role: "null"`) where you meant the text, and remove the
  argument where you meant none. A quoted value is unchanged.
- Breaking: registering a predicate named like a DSL keyword (`any`, `all`, `none`, `between`, `if`, `exactly` and the rest of the
  reserved words, in any case) now throws an `ArgumentException` that names the word from `PredicateRegistryBuilder<TContext>`,
  and the source generator reports the new build error `TWG007` for a `[Predicate]` method with such a name. Rule text could
  never call these names, because the parser read the word as the keyword. Migration: rename the predicate. Rules that called
  the old name need the same rename.
- An array argument given as `from(...)` that matches no node still resolves to an empty array and records no fault, and the
  term's trace text now ends with `[no match for from("source", "query")]`. A mistyped path is visible in the `Decision`.
- `CompilationResult<TContext>` has a `Rule` property, which is the compiled rule (or `null`) and is known to be not `null` after a
  `Succeeded` check, and a `GetRuleOrThrow()` method, which returns the rule or throws an `InvalidOperationException` that lists
  each error diagnostic. The docs and samples use them in place of `CompiledRule!`. `CompiledRule` is unchanged.
- `CompiledRule.PrintRuleText` takes its `GroupingStyle` as an optional argument that defaults to `Parentheses`, so `PrintRuleText()`
  equals `CanonicalText`. The XML docs of the `Decision` overloads of `PrintMermaid` and `PrintPlainText` list the exceptions for a
  decision with no trace tree (`ArgumentException`) and for one that does not match the rule (`InvalidOperationException`).
- Breaking: `EqualsConfigurable` and `NotEqualsConfigurable` in `StringPredicates` are case-sensitive by default. The
  `ignoreCase` argument defaults to `false` (it was `true`), so every string comparison in the catalog is ordinal and
  case-sensitive unless the rule opts in. Migration: add `ignoreCase: true` to each `EqualsConfigurable` or
  `NotEqualsConfigurable` call that relied on the old default.
- Every `NotX` twin in `TruthWeaver.Predicates` is the strict Strong Kleene complement of its positive predicate for a null
  selected value too. Under `NullBehavior.False` the positive predicate answers `False` and its twin now answers `True`
  (the `StringPredicates`, `RegexPredicates`, `NumericPredicates` and `ScalarPredicates` twins answered `False` before).
  This covers `NotEqual`, `NotContains`, `IsNotEmpty`, `NotMatches`, the numeric and scalar `NotEqual`, `NotIn`,
  `Outside` and `IsNotDefault`, and `GreaterThanOrEqual` and `LessThanOrEqual` (the twins of `LessThan` and
  `GreaterThan`). Under `NullBehavior.Unknown` both members still answer `Unknown`. Positive predicates and the definite
  null tests are unchanged.
- Breaking: every built-in predicate in `TruthWeaver.Predicates` answers `Unknown` for a null selected value by default
  (`NullBehavior.Unknown`), because it cannot evaluate a missing value. `Equals`, `EqualsIgnoreCase`, `StartsWith`,
  `EndsWith`, `Contains` and `EqualsConfigurable` in `StringPredicates`, `RegexPredicates.Matches` and
  `CollectionPredicates.SetEquals` defaulted to `NullBehavior.False` and answered `False` (`SetEquals` read a null
  collection as empty, so it answered `True` for an empty argument array). Their twins `NotEqual`, `NotEqualsIgnoreCase`,
  `NotStartsWith`, `NotEndsWith`, `NotContains`, `NotEqualsConfigurable`, `NotMatches` and `NotSetEquals` default to
  `NullBehavior.Unknown` too, so both members of a pair registered with the defaults answer `Unknown`. Migration: pass
  `nullBehavior: NullBehavior.False` at registration to keep the earlier answer; the twin then answers `True`, and
  `SetEquals` keeps the empty-set reading. The definite null tests are unchanged.
- `DateTimePredicates.Between` and `Outside` throw the same reversed-bounds `ArgumentException` as the numeric range
  predicates: `ParamName` is `args` (it was `lower`) and the message is
  `Predicate '<name>' has reversed bounds: 'lower' (<lower>) is greater than 'upper' (<upper>).`, with both bounds in the
  round-trip `O` format of the invariant culture. The `lower` and `upper` argument descriptions are now
  `The inclusive lower bound (date-time).` and `The inclusive upper bound (date-time). It must not be less than the lower
  bound.`

- Breaking: `IDataSource.ScopeAsync` returns `ValueTask<DataScopeResult>` instead of `ValueTask<IDataSource>`. A scope query that is
  malformed, matches no node, matches several nodes or reaches an unsupported node is a failure result, no longer an
  `ArgumentException` or `InvalidOperationException`. Migration: read `result.Source` when `result.Succeeded`, otherwise
  `result.ErrorKind` and `result.ErrorMessage`; an implementation returns `DataScopeResult.Success(source)` or
  `DataScopeResult.Failure(kind, message)`. `FakeDataSource` returns a `NoMatch` failure for a scope nobody scripted (it threw
  before). [ADR-0006](docs/adr/0006-data-sources-for-expression-variables.md) decision 8 is amended in place.
- Every break below. Each one has a migration step in the sections that follow.
- `CompiledRule<TContext>.EvaluateAsync` is one method: `EvaluateAsync(context, services = null, dataSources = null, options = null, cancellationToken = default)`.
  It replaces the two overloads. A caller supplies only what the rule needs, and a null `services` is an empty provider
  (a class-based predicate yields `Unknown` plus a `Fault`). Parameter order: `services` stays second and `dataSources`
  third, so existing `(context, services, dataSources, ...)` calls and all `cancellationToken:` calls compile unchanged.
  A call that passed `options` as the third positional argument must name it: `EvaluateAsync(context, services, options: options)`.

### Fixed

- `CHANGELOG.md` and `docs/predicate-conventions.md` held a stray Windows-1252 byte (0x85) in place of an ellipsis, so they were not valid UTF-8. Both are UTF-8 now, and `TextFileEncodingTests` fails for any Markdown, MSBuild, C#, JSON or YAML file that is not valid UTF-8 or starts with a byte order mark. No migration step.
- `RuleBuilder.Predicate` accepts `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` (and any other `IEnumerable` of
  supported values) as an array-valued argument. Before, these value-type arrays threw `ArgumentException`.

### Breaking changes: naming cleanup (ADR-0007)

Public names and diagnostic codes were aligned with the glossary in [ADR-0007](docs/adr/0007-naming-cleanup-and-tre-diagnostic-prefix.md). Behavior is unchanged. No `[Obsolete]` forwarders exist; this section is the migration guide.

#### Diagnostic code prefix `BRE` is now `TRE`

"TRE" stands for "Trinary Rule Expression". Every code keeps its number, so the mapping is a prefix swap. Update any filter, suppression list or string comparison that uses the old prefix.

| Old | New | `DiagnosticCodes` member |
| --- | --- | --- |
| `BRE0001` | `TRE0001` | `SyntaxError` |
| `BRE0002` | `TRE0002` | `UnknownPredicate` |
| `BRE0003` | `TRE0003` | `MissingArgument` |
| `BRE0004` | `TRE0004` | `ArgumentTypeMismatch` |
| `BRE0005` | `TRE0005` | `UnknownArgument` |
| `BRE0006` | `TRE0006` | `InfixArityViolation` |
| `BRE0007` | `TRE0007` | `AmbiguousOperatorMixing` |
| `BRE0008` | `TRE0008` | `InvalidThresholdValue` |
| `BRE0009` | `TRE0009` | `MaxDepthExceeded` |
| `BRE0010` | `TRE0010` | `MaxNodeCountExceeded` |
| `BRE0011` | `TRE0011` | `AnalysisSkippedTooManyTerms` |
| `BRE0012` | `TRE0012` | `StructuralTautology` |
| `BRE0013` | `TRE0013` | `StructuralContradiction` |
| `BRE0014` | `TRE0014` | `MalformedTree` |
| `BRE0015` | `TRE0015` | `InvalidEscapeSequence` |
| `BRE0016` | `TRE0016` | `RewriteTooLarge` |
| `BRE0017` | `TRE0017` | `RedundantInspection` |
| `BRE0018` | `TRE0018` | `RedundantCoalesce` |
| `BRE0019` | `TRE0019` | `ConstantIfCondition` |
| `BRE0020` | `TRE0020` | `IdenticalIfBranches` |
| `BRE0021` | `TRE0021` | `VacuousCardinality` |
| `BRE0022` | `TRE0022` | `DuplicateOperands` |
| `BRE0023` | `TRE0023` | `DoubleNegation` |

Entries elsewhere in this file that name a code were updated to the `TRE` prefix.

#### Type and member renames

| Old | New |
| --- | --- |
| `EvaluatedNode` | `TraceNode` |
| `Decision.EvaluatedTree` | `Decision.TraceTree` |
| `NodeDescription` (on `TraceEntry` and the tree node) | `Text` |
| `RuleDescription` | `OutlineNode` (the root node `CompiledRule.Outline()` returns is the rule outline) |
| `CompiledRule.Describe()` | `CompiledRule.Outline()` |
| `EvaluationMode.Default` | `EvaluationMode.ShortCircuit` |
| `ResolvedValuePredicates` (`resolve`, `TResolved`) | `SelectedValuePredicates` (`select`, `TSelected`) |
| `UniversalGateExpander` (internal; the public `ExpandToNand` and `ExpandToNor` are unchanged) | `NandNorExpander` |

### Breaking changes at a glance

| Area | Change | Section |
| --- | --- | --- |
| Predicates | `IPredicate<T>.EvaluateAsync` returns `ValueTask<TruthValue>`, not `ValueTask<bool>` | [K3 1](#1-predicates-return-truthvalue) |
| Constants | `ConstantExpression` holds a `TruthValue`; canonical text is `True`/`False`/`Unknown` | [K3 2](#2-constants-are-truthvalue-and-print-capitalised) |
| Biconditional | `XnorExpression` is `EquivalentExpression`; canonical label and JSON/YAML op are `EQUIVALENT`/`equivalent` | [K3 3](#3-xnor-became-equivalent) |
| Diagnostics | `DiagnosticCodes.XorArityViolation` is `InfixArityViolation` (code `TRE0006` unchanged) | [K3 4](#4-the-arity-diagnostic-constant-was-renamed) |
| Analyzer | Tautology and contradiction diagnostics are now Strong K3 results | [K3 5](#5-tautology-and-contradiction-diagnostics-are-k3-results) |
| Predicate names | New operator words are reserved in rule text | [K3 6](#6-new-operator-words-are-reserved) |
| Built-in predicates | `EqualsConfigurable` has no `culture` argument and is ordinal | [K3 7](#7-equalsconfigurable-lost-its-culture-argument) |
| Public records | `CompilerOptions` and `RuleDiffResult` gained positional parameters | [K3 8](#8-records-gained-positional-parameters) |
| Testing | `FakePredicates` answer `Unknown` without a fault | [K3 9](#9-fakepredicates-answer-unknown-without-a-fault) |
| JSON/YAML | New op names; malformed `k` is a diagnostic | [K3 10](#10-jsonyaml-tree-format) |
| Interim names | Names that existed only in unreleased K3 commits | [Interim](#names-that-existed-only-in-unreleased-k3-commits) |

## Migrating across the Strong K3 work

Reference point: commit `57cf2c9`, the last commit before the K3 work began. Each entry gives the old form, the new
form and the migration step.

### 1. Predicates return `TruthValue`

- **Old:** `ValueTask<bool> EvaluateAsync(TContext, PredicateArguments, CancellationToken)`; predicate factories returned
  `Func<..., ValueTask<bool>>`; an indeterminate answer had to be signalled by throwing.
- **New:** `ValueTask<TruthValue> EvaluateAsync(...)`; factories return `Func<..., ValueTask<TruthValue>>`. Return
  `TruthValue.Unknown` for a legitimately indeterminate answer (no `Fault` is recorded); throw only for a real failure
  (recorded as a `Fault`, the term is `Unknown`).
- **Migrate:** change the return type and map the result, for example
  `ValueTask.FromResult(ok ? TruthValue.True : TruthValue.False)`. Registered delegates change the same way. Predicates
  that threw to mean "cannot tell" can now return `Unknown` instead.

### 2. Constants are `TruthValue` and print capitalised

- **Old:** `ConstantExpression(bool Value)`; canonical text `true` / `false`.
- **New:** `ConstantExpression(TruthValue Value)`; canonical text `True` / `False` / `Unknown`. `RuleBuilder.Constant(bool)`
  still exists, and `Constant(TruthValue)` was added.
- **Migrate:** pass `TruthValue` where you build or match `ConstantExpression`. Rule text is unaffected on input (the
  words are case-insensitive), and JSON/YAML keep `true`/`false` booleans; `Unknown` is written `"unknown"`. Anything that
  stored or compared `CanonicalText` (cache keys, golden files, de-duplication) must be regenerated, because the text
  changed for every rule containing a constant.

### 3. `XNOR` became `EQUIVALENT`

- **Old:** `XnorExpression`; canonical text `a XNOR b`; JSON/YAML op `xnor`; `RuleBuilder.Xnor`.
- **New:** `EquivalentExpression`; canonical text `a EQUIVALENT b`; JSON/YAML op `equivalent`; `RuleBuilder.Equivalent`.
  `XNOR` and `IFF` (and `↔`) remain accepted on input in rule text, JSON and YAML, and `RuleBuilder.Xnor` forwards to
  `Equivalent`, so existing rules keep compiling.
- **Migrate:** rename `XnorExpression` to `EquivalentExpression` in code. Regenerate any stored canonical text or printed
  JSON/YAML (see entry 2); the printers now write only the new spelling.

### 4. The arity diagnostic constant was renamed

- **Old:** `DiagnosticCodes.XorArityViolation`.
- **New:** `DiagnosticCodes.InfixArityViolation`. The code string `TRE0006` is unchanged. It now also covers `EQUIVALENT`,
  `IMPLIES`, `NAND` and `NOR` given other than two operands.
- **Migrate:** rename the constant. Comparisons against the string `"TRE0006"` need no change.

### 5. Tautology and contradiction diagnostics are K3 results

- **Old:** the analyzer treated rules as two-valued, so `a OR NOT a` was reported as a tautology and `a AND NOT a` as a
  contradiction.
- **New:** the analyzer is dual-rail Strong K3. A rule is reported only when it is `True` (or `False`) for every
  `True`/`False`/`Unknown` assignment, so `a OR NOT a` is no longer flagged: it is `Unknown` when `a` is. The diagnostic
  codes and constant names are unchanged.
- **Migrate:** none in code. Expect fewer warnings; do not rely on the old two-valued findings.

### 6. New operator words are reserved

- **Old:** only `AND`, `OR`, `NOT`, `XOR`, `XNOR`, `ExactlyOne` and the threshold names were reserved.
- **New:** `IMPLIES`, `EQUIVALENT`, `IFF`, `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `If`,
  `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown` and the constants `True`/`False`/`Unknown` are keywords, matched
  case-insensitively. A predicate registered under one of these names can no longer be used by that bare name in rule
  text (a registered predicate named `any` now fails to parse `any`).
- **Migrate:** rename the colliding predicate; persisted rules that used the old name must be rewritten.

### 7. `EqualsConfigurable` lost its `culture` argument

- **Old:** `EqualsConfigurable(value, ignoreCase, culture, trim)` honoured a `culture` string.
- **New:** comparison is ordinal; the arguments are `value`, `ignoreCase` (default `false`) and `trim` (default `false`).
  A rule that passes `culture` is rejected with an unknown-argument diagnostic that advises removing it. (Between the
  two it briefly required `culture` to be empty; that interim form never shipped.)
- **Migrate:** delete `culture:` from the rule. If you relied on culture-sensitive matching (for example the Turkish
  dotless I), compare in your own predicate instead.

### 8. Records gained positional parameters

- **Old:** `CompilerOptions(MaxDepth, MaxNodeCount, MaxAnalysisTerms, Mode)` and `RuleDiffResult(entries)`.
- **New:** `CompilerOptions` adds `MaxRewriteNodeCount = 100_000` and `Lints = LintRules.None`; `RuleDiffResult` adds
  `bool? PreservesMeaning`.
- **Migrate:** source code that uses named or `with` construction is unaffected. Positional construction and
  deconstruction need the new parameters, and assemblies compiled against the old shape must be rebuilt.

### 9. `FakePredicates` answer `Unknown` without a fault

- **Old:** `FakePredicates.Returning(TruthValue.Unknown)` and a `Scripted` `Unknown` entry threw
  `SimulatedPredicateFaultException`, which recorded a `Fault`.
- **New:** they return `Unknown` directly, like a real predicate, and record no `Fault`. Use `FakePredicates.Faulting`
  to simulate a failure.
- **Migrate:** tests that asserted a fault for an `Unknown` fake should switch to `Faulting`.

### 10. JSON/YAML tree format

- **New op names** (all accepted case-insensitively on input): `equivalent` (`iff`, `xnor`), `implies`, `nand`, `nor`,
  `parity`, `any`, `all`, `none`, `between`, `coalesce`, `if`, `isTrue`, `isFalse`, `isUnknown`, `isKnown`. The printers
  write `equivalent` rather than `xnor`. The constant leaf also accepts the strings `"true"`, `"false"` and `"unknown"`.
  `rule-tree.schema.json` was updated; re-validate documents that embed an older copy of the schema.
- **Malformed `k`:** a non-numeric, fractional or out-of-range `k` on a threshold node is now reported as a diagnostic by
  the JSON and YAML parsers instead of throwing from the parse (a fractional or oversized number used to surface as an
  exception). Code that caught that exception should read `CompilationResult.Diagnostics`.
- **Also new:** tree diagnostics carry a JSON Path style `Path`, and JSON diagnostics also carry a source span.
- **Migrate:** persisted `xnor` documents keep working. Regenerate any golden files that were produced by the printers.

### Names that existed only in unreleased K3 commits

These were added and then replaced during the K3 effort, before any release. They are listed because a clone taken
mid-effort may use them; a consumer starting from `57cf2c9` or `df8f4b9` never saw them.

| Interim form | Final form | Why |
| --- | --- | --- |
| `NXOR` operator (n-ary parity) | `PARITY`; the old spelling is rejected with a diagnostic that names `PARITY` | `NXOR` conventionally means negated XOR |
| `Collapse(expr, policy)` rule operator; `Decision.Outcome`; `CompiledRule.CollapsePolicy` | `Decision.Collapse(CollapsePolicy)` on the result; the rule language no longer has it and rejects it | A rule cannot carry an evaluation boundary |
| `Project(expr, True\|False)` rule operator | `Decision.Project(...)` on the result; rejected in rule text | Same reason |
| `CompiledRule.PrintText(GroupingStyle)` | `CompiledRule.PrintRuleText(GroupingStyle)` | Naming pass |
| `ExpandToPrimitives()`, `ExpandToNand()`, `ExpandToNor()` returning `CompiledRule<TContext>` | Each takes an optional `CompilerOptions` and returns `CompilationResult<TContext>`; read `.CompiledRule`. An over-cap result is a `TRE0016` error with no rule | Size guard (`MaxRewriteNodeCount`) |
| JSON/YAML keys `collapse` and `project` | Rejected with a diagnostic | As above |
| `If` printed with its word label in C-style trees | `If` prints as `?:` in C-style trees | Notation pass |

## Migrating from the first engine commit

Reference point: `df8f4b9`. In addition to everything under [Migrating across the Strong K3 work](#migrating-across-the-strong-k3-work),
these breaks landed between that commit and `57cf2c9`:

- **Rename `BooleanRulesEngine` to `TruthWeaver`** (commit `4cfc1b9`). Solution, project, package and namespace names
  changed (`BooleanRulesEngine.*` to `TruthWeaver.*`), including the Predicates and Testing packages and the
  `BooleanRulesEngine.Ast` namespace. **Migrate:** update `PackageReference`/`ProjectReference` entries and `using`
  directives.
- **Predicate `Description` and `Label` are required** (commits `4176ea2`, `dc02e7b`). `PredicateSchema` and
  `PredicateArgumentSchema` require a `Description`, and `PredicateSchema` requires a `Label`. **Migrate:** supply both
  when building schemas.
- **Canonical text parenthesises mixed `AND`/`OR`** (commit `578cce8`). An `AND` operand under an `OR` (and the reverse)
  is parenthesised even where precedence would make it unambiguous. **Migrate:** regenerate stored canonical text.
- **String literal escaping** (commit `f3272f1`). `LiteralValue.ToString()` and the canonical printer escape `\` and
  `"` in string arguments, and an unknown escape such as `\q` in rule text is now diagnostic `TRE0015` instead of the
  backslash being dropped silently. **Migrate:** fix rule text that relied on an unrecognised escape.

Other changes in that range were additive (threshold family, `XNOR`/`IFF`, `RuleBuilder`, Guid literals,
`Describe()`, rule diff, JSON Schema, metrics, `OperatorStyle`, and the new Predicates, Testing and Yaml packages).

## How this list was verified

Each entry was checked against `git log` and `git diff 57cf2c9 HEAD` and, where it describes behaviour, by running it
(for example a registered predicate named `any` no longer parses, the JSON printer writes `equivalent`, and canonical
text prints `EQUIVALENT`). The interim-name table was checked against the k3-followups tickets 04, 05, 06, 08, 20, 21
and k3-hardening ticket 03. The README quick-start was compiled in a fresh project against the current API.
