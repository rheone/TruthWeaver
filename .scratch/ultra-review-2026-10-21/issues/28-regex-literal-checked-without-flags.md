# 28 - A literal regex pattern is checked with no options when the flags come from a data source

Status: needs-triage
Severity: medium
Lens: F7 Predicates B
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - compiling `isMatch(pattern: "^a+ # one or more (a's", flags: from("request", "$.flags"))` fails with TRE0026 ("Not enough )'s") because `ValidateArguments` validates the literal pattern under `RegexOptions.None` without checking `args.IsDeferred("flags")`, even though the pattern is valid under the `x` flag the data source would supply at evaluation.
Blocked by: none

## Finding F7.3: Literal regex pattern is checked with no options when the flags come from a data source, so a valid rule fails to compile

Location: `src/TruthWeaver.Predicates/RegexPredicates.cs:233` (PLAUSIBLE, medium)

ValidateArguments reads the flags with TryGetString. An omitted flags argument is filled with its default "", so a missing flags value at compile time means it is deferred to a data source. In that case `options` stays RegexOptions.None and the literal pattern is still validated with Cache.Validate. regex-ismatch.md says a data-source flags value is checked at evaluation and that a pattern valid only with 'x' compiles. The validator should skip the pattern check when args.IsDeferred("flags").

### Failure scenario

Rule: `codeIsMatch(pattern: "^a+ # one or more (a's", flags: from("request", "$.flags"))`, where the data source supplies "x". Under 'x' the '(' is inside a comment and the pattern is valid. Under None the paren is unbalanced, so the compiler reports TRE0026 and rejects a rule that would evaluate correctly.

### Expected behavior

When flags is deferred, do not validate the literal pattern at compile time. Leave the check to evaluation, where the real options are known.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
