# 21 - Reading a YAML variables or case document with an unclosed flow sequence throws instead of giving a diagnostic

Status: done
Category: bug
Severity: medium
Slice: Tool.Core (src/TruthWeaver.Tool.Core/Assignment/, src/TruthWeaver.Tool.Core/RuleCases/)
Lens: error handling
Source: found by the coverage-gaps ticket 17 slice (2026-11-02); the other two readers were confirmed by reading the code in the code review

## Finding

`AssignmentVariables.Read("user: [1, 2\nother: x\n")`, a YAML document with an unclosed flow sequence, throws `InvalidOperationException` from `YamlDotNet.Core.Scanner.ScanPlainScalar`. The reader is meant to give a `MalformedDocument` diagnostic for a document it cannot read, and a Tool.Core operation does not throw for an expected failure (docs/tool-core.md, "Operations").

The catch list is too narrow in three readers:

- `AssignmentVariables.TryReadAny` (`Assignment/AssignmentVariables.cs`) catches `JsonException`, `YamlException` and `ArgumentException`.
- `StubAssignment.ReadYaml` (`Assignment/StubAssignment.cs`) catches only `YamlException`.
- `RuleTestCaseText.Parse` (`RuleCases/RuleTestCaseText.cs`) catches only `YamlException`.

The terminal UI and the CLI read user-typed documents through these readers, so a typing mistake can reach them.

## Agent Brief

**Category:** bug
**Summary:** Make the three YAML readers of Tool.Core turn a scanner exception into the same malformed-document diagnostic that they give for a `YamlException`.

**Current behavior:** A YAML document that makes YamlDotNet throw `InvalidOperationException` (the repro is `user: [1, 2` followed by `other: x`) escapes `AssignmentVariables.Read`, `StubAssignment` and `RuleTestCaseText.Parse` as an exception.

**Desired behavior:** Each reader returns its usual failure result: a `MalformedDocument` (or the reader's existing malformed-document) diagnostic that names the problem, with the line and column when the exception gives them. No other exception type changes behavior. A document that already gives a `YamlException` diagnostic is unchanged.

**Acceptance criteria:**
- [x] A test fails before and passes after, for each of the three readers: the unclosed flow sequence above gives a diagnostic and does not throw.
- [x] Existing tests of the three readers pass unchanged.
- [x] Check the other YAML readers in `src/TruthWeaver.Tool.Core` and `src/TruthWeaver.Yaml` for the same narrow catch (grep `YamlException`), and either fix them in the same way or list them in the ticket Result with the reason they are safe.
- [x] `docs/tool-core.md` needs no change unless a diagnostic code changes. A `CHANGELOG.md` `[Unreleased]` entry under Fixed. The validation list in `CLAUDE.md` passes.

**Out of scope:** Changing the YAML parser, or catching `Exception` in general.

## Result

Done on branch `fix/coverage-gaps-2026-10-25`. Each reader gets a catch for `InvalidOperationException` (the YamlDotNet scanner exception, which has no position) that gives the reader's usual malformed result. A red test came first for each reader.

Readers fixed: `AssignmentVariables.TryReadAny`, `StubAssignment.ReadYaml`, `RuleTestCaseText.Parse` (the three named), plus the readers that the `YamlException` grep found and a probe showed to throw: `YamlTreeParser.Parse` (so `YamlRuleDefinitions.ReadYaml` and `CompileYaml`), `YamlDataSource.TryParse`, `PredicateSchemaFile.Parse`, `TreeValue.ReadYaml` (so `TreeSchemaValidator.Validate`), `EmbeddedRuleReader.Select` (so `RuleLoader` with a path), `RuleTestFileReader.Read`, `ConversionNotice.CountYamlComments` and `SourceNodeIndex.Create`. `YamlTreeParser`'s two event-scan helpers (`FindDuplicateArgument`, `ContainerPathAt`) also catch it, because they run on the same text.

Not changed: `YamlDataSource.Parse` still throws, as its documentation says; a caller that wants no exception uses `TryParse`. `YamlDataSource.Create` and the `YamlException` catch on a cyclic alias are about nodes, not text, and are unaffected.
