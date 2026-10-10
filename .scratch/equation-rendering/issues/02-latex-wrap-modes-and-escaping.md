# 02: LaTeX dialect with raw, `$$` and MathJax-safe wrap modes

**What to build:** The LaTeX dialect (`\land`, `\lor`, `\lnot`, `\oplus`, `\leftrightarrow`) with three wrap modes: none, `$$...$$`, and MathJax-safe. Escaping is its own tested unit, as `MermaidTreePrinter.Escape` is.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Predicate names and string-literal arguments escape `_ % & # { } \ ^ ~` correctly in every wrap mode
- [ ] The MathJax-safe mode uses only commands in the MathJax subset that GitHub renders, and handles the underscore-as-subscript quirk inside `$...$`
- [ ] Each wrap mode has a test that renders a rule whose labels contain every special character
- [ ] Tests cover function-call fallback for operators with no LaTeX symbol (`\text{AtLeast}_2(a, b, c)` or the chosen form, documented)
- [ ] Public API has XML docs and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
