# 85 - CompilerOptions.Library changes the record's signature and compares by reference

Status: done
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

## Decision (owner, 2026-11-01)

Give `RuleLibrary` value equality: two libraries with the same names and texts are equal and share a hash code, so `CompilerOptions` compares by content. The added constructor parameter stays.

## Agent Brief

**Category:** enhancement
**Summary:** Make two `RuleLibrary` objects with the same rules equal, so equal `CompilerOptions` compare equal.

**Current behavior:** `RuleLibrary` has reference equality; two options objects with identical settings and two separately built but equal libraries compare unequal.

**Desired behavior:** `RuleLibrary` overrides `Equals` and `GetHashCode` (and `IEquatable<RuleLibrary>`) over its names (compared without case, as the library does) and rule texts. Order of insertion does not matter. `CompilerOptions` equality follows.

**Acceptance criteria:**
- [x] A test fails before and passes after: two libraries built from the same rules in a different order are equal with equal hash codes, and two `CompilerOptions` holding them are equal.
- [x] Libraries that differ in one text are not equal.
- [x] XML docs say how equality works.
- [x] The validation list in CLAUDE.md passes.

## Resolution (2026-11-01)

`RuleLibrary` now implements `IEquatable<RuleLibrary>` with `Equals` and `GetHashCode` over the names (compared without case) and the exact rule texts. Insertion order does not matter, because the equality looks each name up and the hash code is a sum. `CompilerOptions` equality follows through the record. Updated `docs/rule-text.md` (Library rules) and `docs/glossary.md`.

Tests: `RuleLibraryTests.Equals_SameRulesInAnotherOrder_AreEqualAndMakeEqualCompilerOptions_Test` and `Equals_LibrariesThatDiffer_AreNotEqual_Test`. Red first: the first failed with `Assert.Equal() Failure: Values differ` before the change.
