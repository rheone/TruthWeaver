# 31: Resolve ternary precedence wording against `AmbiguousOperatorMixing`

**What to build:** ADR-0005 says the ternary is the lowest-precedence construct, yet `a AND b ? c : d` is rejected as `AmbiguousOperatorMixing`. Those two cannot both stand. Decide which changes: the ADR wording (the ternary is not an operand of a bare infix expression and must be parenthesised) or the parser (a bare ternary after infix operands is accepted at lowest precedence). This is issues-log row 18. Grill the owner first; then make the ADR, parser, diagnostics and tests agree.

**Blocked by:** None (30 is done)

**Status:** ready-for-agent

- [x] The owner decided (2026-10-03): keep the parser strict, so `a AND b ? c : d` stays `AmbiguousOperatorMixing`. Precedence stays `NOT` > `AND` > `OR`, every other infix operator and the ternary need parentheses when mixed, and no parser change is made
- [ ] A test pins that a bare ternary after infix operands is `AmbiguousOperatorMixing` (it may already exist, in which case say so)
- [ ] ADR-0005 decision 13 is reworded to "the ternary is not an operand of a bare infix expression and must be parenthesised", and the ADR, the log row and the diagnostic text describe the same rule
- [ ] The full validation from CLAUDE.md passes

Source: review of PR #4, Spec axis, questionable item on ADR line 424. Related: [21](21-if-cstyle-tree-spelling.md).
