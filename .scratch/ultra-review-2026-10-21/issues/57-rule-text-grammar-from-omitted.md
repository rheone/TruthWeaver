# 57 - The rule-text grammar omits the from(...) variable-reference argument value

Status: needs-triage
Severity: low
Lens: F2 Parsing
Verdict: PLAUSIBLE
Blocked by: none

## Finding F2.11: Rule-text grammar omits the from(...) variable-reference argument value

Location: `docs/rule-text.md:52` (PLAUSIBLE, low)

The EBNF gives `argument = identifier ":" literal` and lists literals as string, number, true, false or an array. The parser also accepts `from("source", "query")` as a whole argument value (DslParser.cs:843, 851-881, ADR-0006 decision 10). The grammar in the docs is incomplete. Here the doc is the side that is wrong.

### Failure scenario

An author or tool writer who follows the published grammar treats `limit: from("limits", "$.max")` as invalid, though the engine accepts it. They also cannot tell from the grammar that from(...) is not allowed inside an array.

### Expected behavior

Add `argument = identifier ":" ( literal | variable )` and `variable = "from" "(" string "," string ")"` to the grammar.

### Verification

not verified (medium or low)

Verified 2026-10-24: reproduced - docs/rule-text.md:50-52 defines `argument = identifier ":" literal` with no variable alternative, while `DslParser.ParseArgument` (src/TruthWeaver/Parsing/DslParser.cs:838-844) calls `ParseVariableReference()` whenever `IsVariableReferenceStart()` matches `from(`, so `limit: from("limits", "$.max")` parses successfully even though the grammar block does not list it.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
