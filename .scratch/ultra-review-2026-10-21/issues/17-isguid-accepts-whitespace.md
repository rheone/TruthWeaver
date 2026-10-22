# 17 - IsGuid accepts surrounding whitespace, but the doc says it does not trim

Status: needs-triage
Severity: medium
Lens: F7 Predicates B
Verdict: CONFIRMED
Blocked by: none

## Finding F7.2: IsGuid accepts surrounding whitespace, but the doc says it does not trim

Location: `src/TruthWeaver.Predicates/TypePredicates.cs:661` (CONFIRMED, medium)

GuidTest calls Guid.TryParse, and Guid.TryParse trims leading and trailing whitespace before it parses. type-isguid.md and type-isnotguid.md both state 'The text must be a whole GUID. This predicate does not trim surrounding spaces.' Code and the ground-truth doc give different answers. The doc reads as the intended rule (one spelling, no guessing).

### Failure scenario

String selector returns "  d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f ". The doc says IsGuid is False and IsNotGuid is True. The code answers IsGuid True and IsNotGuid False. The object-selector overload behaves the same way for a string.

### Expected behavior

Reject text with leading or trailing whitespace, for example by checking that the text is unchanged after a trim before calling Guid.TryParse. The alternative is to change both docs to say whitespace is trimmed.

### Verification

src/TruthWeaver.Predicates/TypePredicates.cs:659-667 shows GuidTest(string) is just Guid.TryParse(value, out _), with no whitespace check. GuidTest(object) calls it for a string, so both overloads behave the same. IsGuid and IsNotGuid (lines 45, 77, 109, 141) all route through GuidTest. docs/strong-k3/predicates/type-isguid.md:71 and type-isnotguid.md:71 both say "This predicate does not trim surrounding spaces." The .NET Guid parser trims leading and trailing whitespace before it parses. I know this from the BCL implementation and did not run it here. So "  d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f " gives IsGuid=True and IsNotGuid=False. The doc says False and True. The code and the ground-truth doc disagree. The impact is small: it is a lenient accept of padded GUID text, not a crash or a wrong K3 truth table. I rate it medium.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
