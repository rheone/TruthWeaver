# 72 - A misspelled quantifier `IN` keyword cascades into follow-on diagnostics, unlike an omitted one

Status: done
Category: bug
Severity: low
Lens: F2 Parsing
Verdict: CONFIRMED
Blocked by: none

## Finding: `DslParser.ExpectIn`'s failure recovery lets a misspelled `IN` cascade, but an omitted `IN` does not

Location: `src/TruthWeaver/Parsing/DslParser.cs` (`ExpectIn`, `ParseQuantifier`)

Surfaced while adding coverage for `ExpectIn`'s error path (coverage-gaps-2026-10-25 ticket 07). `ExpectIn`
reports its `SyntaxError` and returns without consuming the offending token, so whatever token sits where
`IN` belongs is left for `ParseQuantifier`'s next step, `ExpectIdentifierName("a collection name")`, to
consume. When the token in `IN`'s place is itself a plausible identifier, that step silently "recovers" by
treating it as the collection name instead - which happens to produce exactly one diagnostic when the real
collection name was written where `IN` should have been (the omitted-keyword case: `FORALL(x items,
body)`), but produces a second, unrelated diagnostic when an actual misspelling of `IN` is written before
the real collection name (`FORALL(x INN items, body)`): the misspelling is consumed as the collection name,
and the grammar then expects a `,` where the real collection name (`items`) sits, cascading into an
"Expected ',' but found 'items'." error, which in turn cascades into an unclosed-group error and an
"Unexpected token" error at the top level - four diagnostics for one mistake.

Ticket 55 (`wontfix`, closed as already fixed) covered a different pair of parser-cascade findings (a
primary-fallback consuming a group's closer, and a bad threshold bound not skipping to the next `,`/`)`);
it did not touch `ExpectIn`; this is a distinct cascade path.

### Failure scenario

`DslParser.Parse("FORALL(x INN items, elementTrue)")` raises four diagnostics: the correct "Expected 'IN'
but found 'INN'." error, then "Expected ',' but found 'items'.", then an unclosed-`(` error, then an
"Unexpected token ',' after end of expression." error. By contrast,
`DslParser.Parse("FORALL(x items, elementTrue)")` (the keyword omitted outright) raises exactly the one
expected "Expected 'IN' but found 'items'." error, with no cascade. Both are tested as-is (reflecting
current behavior) in `DslParserTests.cs`'s `Parse_QuantifierMissingIn_RaisesSyntaxErrorExpectedInWithNoCascade_Test`
and `Parse_QuantifierMisspelledIn_RaisesSyntaxErrorExpectedInFirst_Test`.

### Expected behavior

A misspelled `IN` should report the one `SyntaxError` and recover the same way the omitted-keyword case
does, with no follow-on diagnostics. One plausible fix: after reporting the error, if the *next* token
(not the current one) is the identifier `IN`, treat the current token as noise and skip it before
continuing, rather than always falling through to let `ExpectIdentifierName` reinterpret the current token.

## Acceptance criteria

- The failure scenario above no longer happens: `FORALL(x INN items, elementTrue)` raises exactly the one
  "Expected 'IN' but found 'INN'." diagnostic, with no follow-on diagnostics.
- The two existing tests named above are updated to assert the single-diagnostic outcome for both cases.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Comments

> *Filed from a discrepancy found while adding coverage for `DslParser.ExpectIn` (coverage-gaps-2026-10-25
> ticket 07, 2026-10-28): that ticket's own description assumed both the omitted- and misspelled-keyword
> cases were cascade-free, which is true only for the omitted case.*

## Resolution (2026-11-01)

Reproduced: `FORALL(x INN items, elementTrue)` gave four diagnostics. `DslParser.ExpectIn` now recovers after it reports the error: when the token after the offending one is an identifier and the token after that is `,` (the shape `<wrong token> collection ,`), it skips the offending token, so the real collection name is read as the collection. The omitted-keyword case (`FORALL(x items, ...)`, where the next token is `,`) is not skipped and is unchanged. The skip is on token shape, not on the spelling, so a non-identifier in place of `IN` (`EXISTS(x : items, ...)`) also gives one diagnostic. Tests: `DslParserTests.Parse_QuantifierMisspelledIn_RaisesOneSyntaxErrorWithNoCascade_Test` (replaces `Parse_QuantifierMisspelledIn_RaisesSyntaxErrorExpectedInFirst_Test`; two rows, and it checks that `items` is the collection); `Parse_QuantifierMissingIn_RaisesSyntaxErrorExpectedInWithNoCascade_Test` already asserted the single diagnostic and still passes. Page updated: `docs/rule-text.md` (Quantifiers).

All acceptance criteria met: one diagnostic for the misspelled case; both tests assert the single-diagnostic outcome; page updated; validation list run (format-all gates and the commit hook's build and full test suite).
