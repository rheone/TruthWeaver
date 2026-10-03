# 31: Resolve ternary precedence wording against `AmbiguousOperatorMixing`

**What to build:** ADR-0005 says the ternary is the lowest-precedence construct, yet `a AND b ? c : d` is rejected as `AmbiguousOperatorMixing`. Those two cannot both stand. Decide which changes: the ADR wording (the ternary is not an operand of a bare infix expression and must be parenthesised) or the parser (a bare ternary after infix operands is accepted at lowest precedence). This is issues-log row 18. Grill the owner first; then make the ADR, parser, diagnostics and tests agree.

**Blocked by:** 30 (the ADR and log must agree on row 18's status first)

**Status:** needs-owner-decision

- [ ] The owner picks the behaviour for `a AND b ? c : d`
- [ ] A failing test pins the chosen behaviour before any change
- [ ] ADR-0005, the parser and the diagnostic text describe the same rule
- [ ] The full validation from CLAUDE.md passes

Source: review of PR #4, Spec axis, questionable item on ADR line 424. Related: [21](21-if-cstyle-tree-spelling.md).
