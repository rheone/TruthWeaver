# 88 - Two libraries whose texts differ only in line endings have one hash but are not equal

Status: ready-for-agent
Category: enhancement
Severity: low
Lens: Follow-up to tickets 84 and 85
Blocked by: none

## Finding

Ticket 84 made the content hash read CRLF and lone CR as LF. Ticket 85 gave `RuleLibrary` value equality over names (without case) and the exact rule texts. A library with CRLF text and one with the same text in LF have the same rule hashes but compare unequal, so equal-meaning `CompilerOptions` compare unequal. The agent chose the narrowest behavior.

## Open question for the owner

Should equality follow the normalized text (so it agrees with the hash), or stay exact?

## Next step

Decide; following the normalized text is a one-line change in `RuleLibrary`.

## Decision (owner, 2026-11-01)

`RuleLibrary` equality follows the normalized text: CRLF and lone CR are read as LF before the texts are compared, so equality agrees with the content hash. The hash code follows the same rule.

## Agent Brief

**Category:** enhancement
**Summary:** Make two libraries whose rule texts differ only in line endings equal.

**Current behavior:** Equality compares the exact rule text, so a CRLF library and an LF library with the same rules have equal rule hashes but compare unequal, and so do `CompilerOptions` that hold them.

**Desired behavior:** `Equals` and `GetHashCode` compare names (without case, as now) and rule texts after CRLF and lone CR are read as LF. Any other text difference still makes libraries unequal.

**Acceptance criteria:**
- [ ] A test fails before and passes after: libraries with the same rules in CRLF and in LF are equal with equal hash codes, and so are `CompilerOptions` holding them.
- [ ] Libraries that differ in any other character are not equal.
- [ ] XML docs on `RuleLibrary` state the rule; CHANGELOG mention if equality is already listed there. The validation list in CLAUDE.md passes.
