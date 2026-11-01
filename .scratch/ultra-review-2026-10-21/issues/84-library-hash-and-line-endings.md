# 84 - The library content hash depends on line endings

Status: needs-triage
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
