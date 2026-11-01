# 84 - The library content hash depends on line endings

Status: ready-for-agent
Category: bug
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

`RuleLibrary` hashes the UTF-8 bytes of the rule text as written (SHA-256, upper-case hex), the same as the rule store's `HashOf`. Text with CRLF and the same text with LF give different hashes, so a pin (`rule("x", hash: "...")`) can match in one environment and raise `TRE0048` (`LibraryRuleHashMismatch`) in another.

## Open question for the owner

Should the hash normalize line endings (and then the rule store's hash must change with it, which changes every stored hash), or stay byte-exact and be documented as such? Ticket 11b-5 decides whether the store reads the core's hash, so this should be settled before 11b-5.

## Next step

Decide, then write a brief. Whichever way, the core and the store must produce one value.

## Decision (owner, 2026-11-01)

Normalize line endings before hashing: CRLF and CR are read as LF. The core's hash is the single definition; the rule store switches to it in ticket 11b-5, so stored hashes change with it.

## Agent Brief

**Category:** bug
**Summary:** Make the library content hash independent of line endings.

**Current behavior:** The hash is SHA-256 of the UTF-8 bytes of the rule text as written, so CRLF and LF give different hashes and a pin can fail across platforms.

**Desired behavior:** The hash is SHA-256, as upper-case hex, of the UTF-8 bytes of the text with every CRLF and lone CR replaced by LF. A pin compares without case, as now. Nothing else in the text is changed (no trimming).

**Acceptance criteria:**
- [ ] A test fails before and passes after: a rule with CRLF and the same rule with LF have the same hash, and a pin written from one matches a library holding the other.
- [ ] The hash of an LF-only text is unchanged.
- [ ] The Library rules section of `docs/rule-text.md` and the glossary state the normalization.
- [ ] The validation list in CLAUDE.md passes, with a CHANGELOG entry.

**Out of scope:** Changing the rule store's own hash (ticket 11b-5, which this ticket blocks).
