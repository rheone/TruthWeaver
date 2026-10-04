# 36: Threshold family accepts one operand; correct the README

**What to build:** The operator table, the compiler and the documentation agree that `AtLeast`, `AtMost`, `Exactly`, `GreaterThan` and `LessThan` accept a single operand (the compiler already does, and the simplifier folds a one-operand threshold to the operand or its negation). The table's minimum changes from 2 to 1, a test pins the one-operand behaviour, and the README is corrected both for the threshold-family claim and for the n-ary AND/OR row, which shows a call form the DSL rejects and should read as the infix chain. The k3-reference documents already describe the compiler's behaviour and need no change.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] A test fails first, then passes, showing the table minimum is 1 for the five threshold operators and unchanged for the others
- [ ] The README no longer claims a minimum of 2 for the threshold family and shows the AND/OR chain in its working infix spelling
- [ ] The k3-hardening 11 note about the arity mismatch is marked resolved
- [ ] The reference verification harness still passes
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
