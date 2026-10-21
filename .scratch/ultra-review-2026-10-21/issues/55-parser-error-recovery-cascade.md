# 55 - DSL error recovery cascades into misleading follow-on diagnostics, and reserved words in operand position give a reserved-name error

Status: needs-triage
Severity: low
Lens: F2 Parsing
Verdict: PLAUSIBLE
Blocked by: none

## Finding F2.8: Error recovery cascades into misleading follow-on diagnostics

Location: `src/TruthWeaver/Parsing/DslParser.cs:720` (PLAUSIBLE, low)

When ParsePrimary meets an unexpected token, it always consumes it (720-723), even when the token is a closer that an enclosing group is waiting for. ParseIntegerBound only skips a NumberLiteral (1148-1151), so a missing k leaves the operand in place, and then ExpectClose and the top level report it again.

### Failure scenario

`(a AND )` reports 'Expected a term...' and then a false 'Unclosed '(' ... expected ')' before the end of the rule', though the paren is closed. `AtLeast(a, b, c)` (k forgotten) gives three errors: InvalidThresholdValue at a, "Expected ')' ... found 'a'", and "Unexpected token 'a' after end of expression".

### Expected behavior

Do not consume a closer that matches an open group. After a bad bound, skip to the next ',' or ')' so that one mistake gives one diagnostic.

### Verification

not verified (medium or low)

## Finding F2.9: Reserved operator words in operand position parse as terms, giving 'reserved predicate name' instead of a syntax error

Location: `src/TruthWeaver/Parsing/DslParser.cs:705` (PLAUSIBLE, low)

Any identifier that does not match a primary keyword falls into ParseTerm. That includes infix words (AND, OR, XOR, IMPLIES, NAND, NOR, EQUIVALENT, IFF, XNOR). The mistake is then reported by the compiler as UnknownPredicate "'OR' is a reserved word and cannot be a predicate name". syntax.md:48 says `AND(a, b)` and `XOR(a, b)` are syntax errors.

### Failure scenario

`a AND OR` (missing operand) and `XOR()` or `XOR(x: 1)` give UnknownPredicate (reserved word) and no SyntaxError, so the message points the author at predicate naming instead of the missing operand or the call form that is not allowed.

### Expected behavior

An infix operator word in operand position is a SyntaxError ('expected a term, constant or (') at that token.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
