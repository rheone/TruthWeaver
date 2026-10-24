# 49 - A multi-document YAML stream silently compiles only the first document

Status: needs-triage
Severity: low
Lens: T2 Surfaces
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - compiler.CompileYaml("const: true\n---\nconst: false\n") succeeds with zero diagnostics and evaluates to True, silently discarding the second document (confirmed with a throwaway xUnit test, then removed).
Blocked by: none

## Finding T2.9: A multi-document YAML stream silently compiles only the first document

Location: `src/TruthWeaver.Yaml/YamlTreeParser.cs:57` (PLAUSIBLE, low)

Parse uses stream.Documents[0] and ignores the rest. The JSON surface rejects trailing content as malformed, so the two tree formats disagree on a stream with more than one document.

### Failure scenario

`const: true\n---\nconst: false\n` compiles to True with no diagnostic. A reviewer who reads the whole file may take the second document as the rule.

### Expected behavior

Report MalformedTree when the stream holds more than one document.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
