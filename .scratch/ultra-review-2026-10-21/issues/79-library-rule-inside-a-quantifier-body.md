# 79 - A library rule used inside a FORALL or EXISTS body is checked at the top-level scope

Status: needs-triage
Category: bug
Severity: low
Lens: Follow-up to ticket 11b-2
Blocked by: none

## Finding

Ticket 11b-2 checks each library rule on its own in the top-level scope. A library rule that is referenced inside a `FORALL` or `EXISTS` body and relies on that body's element predicates (the collection's element registry) gets `TRE0050` (`InvalidLibraryRule`), because those predicates are not in the top-level registry. This case was not tested. ADR-0019 decision 4 says a library rule resolves its names against the caller's compile, which can be read as including the scope of the quantifier body.

## Open question for the owner

Should a library rule referenced inside a quantifier body resolve against that body's element registry (so a library rule can be written for a collection element), or against the top-level registry only (so such a rule is an error by design)?

## Next step

Decide, then write an Agent Brief. `let` definitions already resolve inside a quantifier body against that collection's element registry (see the Quantifiers section of `docs/rule-text.md`), which suggests the first reading.
