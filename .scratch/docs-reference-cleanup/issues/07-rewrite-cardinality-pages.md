# 07: Rewrite cardinality pages

**What to build:** The ten cardinality pages (`AtLeast`, `AtMost`, `Exactly`, `ExactlyOne`, `GreaterThan`, `LessThan`, `ANY`, `ALL`, `NONE`, `BETWEEN`) and their index read as a current reference, under the same rules as ticket 06. The threshold pages state "one or more operands" as fact, with no note about a table mismatch. Valid-range explanations appear once per page, not twice. The cardinality index gains the interval diagram that shows how the definitely-true count d and the possibly-true count p decide `T`, `F` or `U`.

**Blocked by:** 03, 04, 05

**Status:** ready-for-agent

- [ ] The ten pages and the index contain none of the words and links the lint test forbids, and link only inside `docs/strong-k3/`
- [ ] Each page has an "Evaluation behavior" section with observable behavior only, including that these operations do not short-circuit
- [ ] The interval diagram is valid and agrees with the Formal Semantics text
- [ ] Syntax, diagnostic and fault text is stated once and linked
- [ ] The baseline list loses these pages, and the reference harness and the lint test pass
