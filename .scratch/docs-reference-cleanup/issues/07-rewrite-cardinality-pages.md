# 07: Rewrite cardinality pages

**What to build:** The ten cardinality pages (`AtLeast`, `AtMost`, `Exactly`, `ExactlyOne`, `GreaterThan`, `LessThan`, `ANY`, `ALL`, `NONE`, `BETWEEN`) and their index read as a current reference, under the same rules as ticket 06. The threshold pages state "one or more operands" as fact, with no note about a table mismatch. Valid-range explanations appear once per page, not twice. The cardinality index gains the interval diagram that shows how the definitely-true count d and the possibly-true count p decide `T`, `F` or `U`.

**Blocked by:** 03, 04, 05

**Status:** done

- [x] The ten pages and the index contain none of the words and links the lint test forbids, and link only inside `docs/strong-k3/`
- [x] Each page has an "Evaluation behavior" section with observable behavior only, including that these operations do not short-circuit
- [x] The interval diagram is valid and agrees with the Formal Semantics text
- [x] Syntax, diagnostic and fault text is stated once and linked
- [x] The baseline list loses these pages, and the reference harness and the lint test pass

## Comments

- 2026-10-04: Done in one scripted pass followed by manual fixes. Each page lost its ADR, ticket and owner references, the quoted diagnostic messages and the repeated syntax paragraphs. Section names are sentence case and "Implementation Notes" became "Evaluation behavior" with observable behavior only. Bold bullet lead-ins were removed. The developer-only items (evaluator folding order, linear cost, analyzer rails, test and harness remarks, trace-node reporting for non-parameterised operations) were dropped here. Code comments in `Evaluator` and `OperatorDefinitions` already describe the evaluation order, so no comment was added. The cardinality index gained the interval diagram and a table of the `True` and `False` bounds for each operation.
