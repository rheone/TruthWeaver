# C2: Core: validation levels, statistics, diagnostics writers

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** A validation engine with these levels, each switchable and reported separately: syntax and compile diagnostics; JSON schema for the tree (JSON and YAML); limits (depth, size, definitions); strict schema file (predicate names, argument counts and kinds from the optional YAML schema file); analysis (tautology, contradiction, redundancy and the opt-in lints); conversion self-check (text, JSON and YAML give the same tree). Statistics: node count, depth, term count, distinct predicates, operator histogram, variables and data sources used, BDD size, tautology or contradiction status, truth-table size and whether it is within the cap. Diagnostics writers: text with spans, JSON and SARIF 2.1.0 (rule ids from the diagnostic code table, locations from spans). Severity model with exit-code mapping and a strict mode that fails on warnings.

**Blocked by:** C1

**Status:** done

- [x] Each level has pass and fail tests; a level can be switched off
- [x] Statistics are tested against hand-counted rules
- [x] The SARIF output validates against the SARIF 2.1.0 schema in a test (schema file checked in under test support, no network)
- [x] The diagnostics code table is the source of rule ids
- [x] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11, dispatch run: done. Decisions taken without the owner (review welcome):
  - Public API in `TruthWeaver.Tool.Core`: `RuleValidator.Validate(LoadedRule, ValidationOptions?)` returns `ToolResult<ValidationReport>` (the report is always present); `ValidationOptions` (`Levels`, `Lints` = `LintRules.All`, `Settings`, `PredicateSchema`); `ValidationLevels` (flags `Compile`, `TreeSchema`, `Limits`, `PredicateSchema`, `Analysis`, `ConversionCheck`, `All`); `ValidationLevelReport` and `ValidationStatus` (`Passed`, `PassedWithWarnings`, `Failed`, `Skipped` with `Reason`, `Off`); `RuleStatistics.Compute`, `RuleConstancy`, `VariableUse`; `PredicateSchemaFile`; `DiagnosticWriter` and `ValidationReportWriter` with `DiagnosticOutputFormat` (`Text`, `Json`, `Sarif`) and `DiagnosticDocument`; `DiagnosticCodeTable`/`DiagnosticCodeInfo`; `SeverityPolicy` and `ToolExitCode` (`Success` 0, `Failed` 1, `Unusable` 2 for a `TWT` input/config/schema-file/output error).
  - JSON schema level without a new dependency: the spec does not scope a JSON schema library for Core, so Core embeds `src/TruthWeaver/Json/rule-tree.schema.json` (linked, one source) and reads it with a small checker for the ten keywords the schema uses. A test keeps the schema inside that set, and another compares the verdicts with JsonSchema.Net (test project only) over valid and invalid rules. If the owner prefers JsonSchema.Net in Core, the checker can be swapped.
  - "Limits (depth, size, definitions)": the rule language has no definitions, so the limits level is depth (`TRE0009`) and size (`TRE0010`). `TRE0011` (analysis skipped) is in the analysis level. A rule that fails only a limit is compiled again without the limits so the other levels and statistics run. With the limits level off, the depth and node limits are not applied (and the deep-nesting lint cannot fire).
  - The predicate schema file format did not exist; this ticket defines it (YAML: `predicates: {name: {description?, arguments?: {arg: kind | {kind, required?, description?}}}}`, kinds are `LiteralKind` names, any case). The level compiles strictly against it and reports `TRE0002`-`TRE0005` only. A file problem is the new `TWT0012`; a malformed file is `TWT0003`.
  - New codes `TWT0012` `InvalidSchemaFile`, `TWT0013` `TreeSchemaViolation`, `TWT0014` `ConversionMismatch`. A tree schema finding reports the closest `oneOf` branch (fewest findings).
  - SARIF: all `DiagnosticCodeTable` rows (every TRE and TWT code) are the driver rules; a test checks the table against the constants and against the code tables of `docs/strong-k3/specification/diagnostics.md` and `docs/tool-core.md`. Embedded rules get a logical location (rule path + diagnostic path) and no region, because spans are relative to the embedded rule (C4 owns offsets). Standard input has no physical location. The checked-in SARIF schema is SchemaStore's draft-07 copy (`json.schemastore.org/sarif-2.1.0.json`), because the OASIS file is draft-04, which JsonSchema.Net does not read; the log names that URI in `$schema`.
  - The conversion self-check has an internal printer seam (`RuleValidator.ValidateWith`, `InternalsVisibleTo` the test project) so its failure path is testable; the real printers never mismatch (L3).
  - Statistics count operators by their JSON tree-format name from `PrintJson`, because the expression tree is internal to `TruthWeaver`. Truth-table rows are `BigInteger`.
