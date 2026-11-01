# 04 - One completeness test for the language vocabulary

Status: ready-for-agent
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
- [ ] The completeness test exists and was shown red by removing one word from a list, then green.
- [ ] The twelve per-operator tests and the literal keyword pin are deleted only after the new test is shown to cover every word they covered (list them in the Resolution).
- [ ] These stay: the reserved-word parameter tests, the retired-word tests (`NXOR`, `PROJECT`, `COLLAPSE`, the constant letters), `ArbitraryName_ReturnsFalse`, and the table tests.
- [ ] The validation list in CLAUDE.md passes.

**Out of scope:** Deriving the lists from the table (ticket 05).
