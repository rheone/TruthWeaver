# 56 - The TreePath bracket form does not escape backslash, so two different keys can share one path

Status: needs-triage
Severity: low
Lens: F2 Parsing
Verdict: PLAUSIBLE
Blocked by: none

## Finding F2.10: TreePath bracket form does not escape backslash, so two different keys can share one path

Location: `src/TruthWeaver/Parsing/TreePath.cs:19` (PLAUSIBLE, low)

Property escapes a single quote as \' but leaves a backslash as it is. The key a' and the key a\ both become $['a\']. A path no longer identifies one node, and diagnostics, JsonSpanLocator and CompiledRule.Notes are keyed by path.

### Failure scenario

A JSON definitions section with entries named "a'" and "a\\" (or predicate args with those names) gives both diagnostics the path $.definitions['a\'], so the span locator can underline the wrong entry.

### Expected behavior

Escape the backslash before the quote, so each key has its own path.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
