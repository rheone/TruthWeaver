# 08 - Rule-shape reader, with RuleTermList migrated

Status: needs-triage
Category: refactor
Severity: low
Blocked by: 07
Effort: [architecture-deepening-4](../spec.md)

**What to build:** The rule-shape reader from the ticket 07 decision, with `RuleTermList` as the first consumer. The consumer no longer parses `PrintJson` text or knows the key names `predicate`, `args`, `op`, `operands` and `from`.

**Acceptance criteria:**
- [ ] A test through the reader's interface covers terms, arguments, variable references and the chosen definition handling.
- [ ] `RuleTermList` holds no JSON key name.
- [ ] Existing term list tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.
