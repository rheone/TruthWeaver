# 01: Equation printer core and Unicode dialect

**What to build:** A flat, single-line infix rendering of a compiled rule, starting with the plain Unicode dialect. The walk generalises `CanonicalPrinter` (context-aware minimal parenthesisation) over a per-dialect token table. The full term call is the default. An option hides the arguments.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A public entry point prints a `CompiledRule` as `a ∧ (b ∨ c)` with the Symbolic operator vocabulary, with no delimiters and no escaping
- [x] Terms print as the full call by default and as the bare name when arguments are hidden
- [x] `ExactlyOne`, the threshold family and other operators with no infix symbol print in function-call form
- [x] `CanonicalPrinter` and the persisted DSL text are unchanged
- [x] Tests cover each K3 operator, nesting and parenthesisation, and argument hiding
- [x] Public API has XML docs, tests are named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
