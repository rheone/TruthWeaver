# 08: Rewrite functions, result transformations and predicates

**What to build:** The six function pages (`COALESCE`, `If`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`), the two result transformation pages (`Project`, `Collapse`), their indexes and the predicates placeholder read as a current reference, under the same rules as ticket 06. The `If` page loses the note that its category placement is pending. The `predicates/README.md` placeholder states in present tense that the reference does not document individual predicates, and it points to the predicate and term definitions in `terminology.md`, with no "on hold" or ticket language.

**Blocked by:** 03, 04, 05

**Status:** done

- [x] The nine pages, the three indexes and the placeholder contain none of the words and links the lint test forbids, and link only inside `docs/strong-k3/`
- [x] Each operation page has an "Evaluation behavior" section with observable behavior only
- [x] The placeholder describes the current state without status or ticket language
- [x] Syntax, diagnostic and fault text is stated once and linked
- [x] The baseline list loses these pages, and the reference harness and the lint test pass

## Comments

- 2026-10-04: Done in one scripted pass followed by manual fixes. Each page lost its ADR, ticket and owner references, the quoted diagnostic messages and the repeated syntax paragraphs. Section names are sentence case and "Implementation Notes" became "Evaluation behavior" with observable behavior only. Bold bullet lead-ins were removed. The developer-only items (evaluator folding order, linear cost, analyzer rails, test and harness remarks, trace-node reporting for non-parameterised operations) were dropped here. The moved developer detail now lives in doc comments on `Evaluator.EvalChainAsync` (the left-to-right fold and the stop rule) and on the threshold case of `Evaluator` (every operand runs, linear cost). `Evaluator.EvalIfAsync` and the analyzer's `Coalesce` already described their behavior. The `predicates/README.md` placeholder is reworded in present tense and points to the terminology page.
