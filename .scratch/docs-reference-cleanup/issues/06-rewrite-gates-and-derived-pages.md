# 06: Rewrite gates and derived pages

**What to build:** The nine pages for `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR` and `PARITY` read as a current reference. Each page is in present tense, uses the new section names and sentence-case headings, and has no ADR, ticket, `.scratch`, owner or "open question" reference. Repeated syntax and diagnostic text becomes a link to `syntax.md` or `diagnostics.md`. Repeated fault and short-circuit text becomes a link to `evaluation.md`, and the page keeps only behavior specific to its Operation. "Implementation Notes" becomes "Evaluation behavior" and keeps observable behavior only. Developer-only items move to code comments in `Evaluator` and `OperatorDefinitions`. The two category indexes and the existing diagrams are updated to match. Every page passes a `/humanizer` pass. Tables, markers, formulas and code blocks stay unchanged.

**Blocked by:** 03, 04, 05

**Status:** done

- [x] The nine pages and the two indexes contain none of the words and links the lint test forbids, and link only inside `docs/strong-k3/`
- [x] Each page has an "Evaluation behavior" section with observable behavior only
- [x] Syntax, diagnostic and fault text is stated once and linked, not repeated
- [x] Hedge words stay only where the behavior is genuinely optional
- [x] Moved developer detail appears as code comments in the engine
- [x] The baseline list loses these pages, and the reference harness and the lint test pass

## Comments

- 2026-10-04: Done in one scripted pass followed by manual fixes. Each page lost its ADR, ticket and owner references, the quoted diagnostic messages and the repeated syntax paragraphs. Section names are sentence case and "Implementation Notes" became "Evaluation behavior" with observable behavior only. Bold bullet lead-ins were removed. The developer-only items (evaluator folding order, linear cost, analyzer rails, test and harness remarks, trace-node reporting for non-parameterised operations) were dropped here. The moved developer detail now lives in doc comments on `Evaluator.EvalChainAsync` (the left-to-right fold and the stop rule) and on the threshold case of `Evaluator` (every operand runs, linear cost). `Evaluator.EvalIfAsync` and the analyzer's `Coalesce` already described their behavior.
