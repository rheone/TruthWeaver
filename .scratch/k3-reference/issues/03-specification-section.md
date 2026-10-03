# 03: Specification section: values, semantics, terminology, notation

**What to build:** The docs/strong-k3/specification/ documents that every operation document links to: values (T, F, U and the truth order F < U < T, with the caution that it is an implementation aid and not a numeric ordering, plus the information order), semantics (Strong Kleene connectives as min/max/negation, truth-functional evaluation versus strongest extension, which classical laws fail such as excluded middle and non-contradiction, which hold such as De Morgan, absorption and double negation, and the invalid consensus removal, plus the strong-K3-connective versus external-operator distinction), terminology (Operation, primary category, primitive, derived, canonical form, public form) and notation (the LaTeX conventions used in every formula). Every claim is verified by brute force.

**Blocked by:** 01, 02

**Status:** done

- [x] Four documents exist and each states its claims with a verified table or counter-example
- [x] The strong versus external operator distinction and the information order are documented
- [x] Notation covers every symbol later documents will use
- [x] The 'classical laws that fail' list matches the spec audit's verified results
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) sections A, C and D. See also [spec](../spec.md).

## Comments

- Delivered `specification/values.md`, `semantics.md`, `terminology.md` and `notation.md` and linked them from the specification index. Truth tables (`NOT`, `AND`, `OR`, `IMPLIES`, `COALESCE`, `IsKnown`, `IsUnknown`), the `AtLeast` evaluation table and the canonical forms (De Morgan, `If` with the consensus term) carry `k3:` markers, so the harness checks them against `K3Oracle`. Laws that hold or fail (commutativity through contraposition, the six failing classical laws, the no-tautology theorem, strongest-extension claims, monotonicity) have no operation to bind to a marker; they were confirmed by an independent exhaustive script over all assignments, not added to the harness.
- Assumption to confirm: the ticket lists "public form" without a definition anywhere in the spec, proposal or ADR. `terminology.md` defines it as the spelling an author or API consumer uses to name an Operation (printed word, JSON/YAML `op`, `RuleBuilder` member), as opposed to the canonical (primitive) form.
- No source or test files changed.
