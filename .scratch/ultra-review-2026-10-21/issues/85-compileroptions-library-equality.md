# 85 - CompilerOptions.Library changes the record's signature and compares by reference

Status: needs-triage
Category: enhancement
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

Ticket 11b-2 added `RuleLibrary? Library` as the last parameter of the positional record `CompilerOptions`. This changes the constructor signature and `Deconstruct` for any caller compiled against the old shape. Record value equality now includes the library by reference, so two options objects with identical settings and two separately built but equal libraries compare unequal, which defeats a cache keyed on options. The library itself is not validated.

## Open question for the owner

Should `RuleLibrary` have value equality (same names and texts are equal), so options compare by content? And is the added constructor parameter acceptable before the first release?

## Next step

Decide, then write a brief; value equality would be a small change inside `RuleLibrary`.
