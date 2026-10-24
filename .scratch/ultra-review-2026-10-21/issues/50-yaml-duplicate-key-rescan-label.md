# 50 - YAML duplicate-key rescan labels any repeat under a key named 'args' as DuplicateArgument

Status: needs-triage
Severity: low
Lens: T2 Surfaces
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - compiler.CompileYaml("definitions:\n  args:\n    body: {const: true}\n    body: {const: false}\n") raises exactly one diagnostic coded DuplicateArgument (TRE0032) instead of MalformedTree, because FindDuplicateArgument's top.Path.EndsWith(".args") check matches the 'args'-named definitions entry, not only a predicate's own args mapping (confirmed with a throwaway xUnit test, then removed).
Blocked by: none

## Finding T2.8: YAML duplicate-key rescan labels any repeat under a key named 'args' as DuplicateArgument

Location: `src/TruthWeaver.Yaml/YamlTreeParser.cs:126` (PLAUSIBLE, low)

FindDuplicateArgument decides it is inside an args mapping with `top.Path.EndsWith(".args")`. A definition named `args`, or a predicate argument named `args` whose value is a variable-reference mapping, also has that path suffix.

### Failure scenario

`definitions:\n  args:\n    body: {const: true}\n    body: {const: false}\n...` reports TRE0032 'Argument 'body' is given more than once' instead of MalformedTree for a repeated definition key.

### Expected behavior

Track that the frame is the 'args' member of a predicate node, not only the path text.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
