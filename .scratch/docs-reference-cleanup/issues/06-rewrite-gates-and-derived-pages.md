# 06: Rewrite gates and derived pages

**What to build:** The nine pages for `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR` and `PARITY` read as a current reference. Each page is in present tense, uses the new section names and sentence-case headings, and has no ADR, ticket, `.scratch`, owner or "open question" reference. Repeated syntax and diagnostic text becomes a link to `syntax.md` or `diagnostics.md`. Repeated fault and short-circuit text becomes a link to `evaluation.md`, and the page keeps only behavior specific to its Operation. "Implementation Notes" becomes "Evaluation behavior" and keeps observable behavior only. Developer-only items move to code comments in `Evaluator` and `OperatorDefinitions`. The two category indexes and the existing diagrams are updated to match. Every page passes a `/humanizer` pass. Tables, markers, formulas and code blocks stay unchanged.

**Blocked by:** 03, 04, 05

**Status:** ready-for-agent

- [ ] The nine pages and the two indexes contain none of the words and links the lint test forbids, and link only inside `docs/strong-k3/`
- [ ] Each page has an "Evaluation behavior" section with observable behavior only
- [ ] Syntax, diagnostic and fault text is stated once and linked, not repeated
- [ ] Hedge words stay only where the behavior is genuinely optional
- [ ] Moved developer detail appears as code comments in the engine
- [ ] The baseline list loses these pages, and the reference harness and the lint test pass
