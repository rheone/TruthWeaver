# 04 - One completeness test for the language vocabulary

Status: done
Category: refactor
Severity: low
Blocked by: none
Effort: [architecture-deepening-3](../spec.md)

## Finding

Each operator has its own test that its word is reserved (twelve positive tests across `AnyAllNone`, `Between`, `Coalesce`, `Equivalent`, `If`, `Inspection`, `Implies`, `NandNor`, `Parity` and the constants), and the keyword list is pinned by a literal test. A new operator needs a new test, and a missed one is how spelling drift got in.

## Agent Brief

**Category:** refactor
**Summary:** Replace the per-operator reserved-word tests with one test driven by the operator table.

**Desired behavior:** One test walks every operator definition and checks that its label, every alias and each word-form symbol is a reserved word and appears in the keyword suggestions, and that the quantifier words and constants are too. The test fails by naming the missing word.

**Acceptance criteria:**
- [x] The completeness test exists and was shown red by removing one word from a list, then green.
- [x] The twelve per-operator tests and the literal keyword pin are deleted only after the new test is shown to cover every word they covered (list them in the Resolution).
- [x] These stay: the reserved-word parameter tests, the retired-word tests (`NXOR`, `PROJECT`, `COLLAPSE`, the constant letters), `ArbitraryName_ReturnsFalse`, and the table tests.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Deriving the lists from the table (ticket 05).

## Resolution

Status: done

`LanguageVocabularyCompletenessTests` walks `OperatorDefinitions.All` (the name in each label, and each symbol spelled as a word) plus the `IFF` and `XNOR` aliases, `FORALL`, `EXISTS`, `Let` and the constants. It checks each word is reserved as written, in lower case and in upper case, and is offered in `DslVocabulary.Keywords`. Shown red by removing `NAND` from `ReservedWords` and `NOR` from `InfixWords`: the failures named `Not reserved: NAND` and `Not offered as a keyword suggestion: NOR`. Both were restored.

The table holds no alias list, so the two aliases are named in the test until ticket 05 derives the lists.

Deleted: the reserved-word tests in `AnyAllNone`, `Between`, `Coalesce`, `Equivalent` (`equivalent`, `iff`, `xnor`), `If`, `Implies`, `Inspection`, `NandNor`, `Parity` (`parity`, `PARITY`) and `TruthValueConstants` (`Unknown`), plus `IsReservedWord_DslKeyword_IsReserved_Test` and `IsReservedWord_EveryKeyword_ReturnsTrueCaseInsensitively_Test`, and the literal `Keywords_Documented_ListsEveryKeywordSpelling_Test`. Every word they listed is a table label, an alias, a constant or a retired word. The retired words (`nxor`, `project`, `collapse`) keep their own tests, as do the constant letters, `ArbitraryName_ReturnsFalse`, `OrdinaryName_IsNotReserved`, `Let_ReturnsTrue` and the table tests.

Review follow-up: the keyword check compares the exact documented spelling, and a second test fails on a keyword that is not a word of the language or is listed twice, which restores the spelling, no-extras and no-duplicates guarantees of the deleted literal pin. Only the list order is no longer pinned.
