# 03: Syntax and diagnostics pages

**What to build:** A rule author finds the grammar and the compile-time errors on one page each. `specification/syntax.md` states precedence, the mixing rule, call versus infix forms, symbol aliases, case rules and the JSON and YAML shape once. `specification/diagnostics.md` lists each `TRE` code that the operations produce with its name, cause and fix, and does not quote message text. Every statement is checked against the real compiler, the same way the earlier probe checked the pages. The checker verifies that both pages resolve their links.

**Blocked by:** 02

**Status:** done

- [x] `syntax.md` covers precedence, the mixing rule, call forms, infix forms, symbol aliases, case rules and the JSON and YAML shape
- [x] `diagnostics.md` lists each code used by the operation pages once, with name, cause and fix
- [x] A throwaway probe or an added test shows each stated syntax and diagnostic claim holds in the compiler
- [x] Both pages follow the documentation standard and link only inside `docs/strong-k3/`
- [x] The reference harness and the lint test pass

## Comments

- 2026-10-04: Done. Every syntax and diagnostic claim was checked with a throwaway compile probe of about 90 rule texts (DSL, JSON and YAML); the probe was deleted afterward. The probe showed that `TRE0011` is Info, and that `a XOR b EQUIVALENT c` is `TRE0007`.
