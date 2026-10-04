# 15: Repo docs: external operators, terminology and ADR amendments

**What to build:** README, CONTEXT.md, ADR-0005 and CLAUDE.md tell the truth about the language. State that only the connectives (NOT, AND, OR, IMPLIES, EQUIVALENT, XOR, NAND, NOR, PARITY, cardinality, ANY/ALL/NONE/BETWEEN, If) are Strong K3, while COALESCE and the four inspection operators are external operators (not information-monotone; SQL and Bochvar precedents) so the 'no tautologies' theorem and NAND/NOR expressiveness do not extend to them. Add the information order beside the truth order, note that 'Project' and 'Collapse' are TruthWeaver terms (not K3 literature) and are methods on the result, record the PARITY rename, add ADR-0005 amendments for the final decisions (Collapse/Project as Decision methods, PARITY, If rationale citing the strongest-extension result), BETWEEN needs min <= max, and use 'Strong Kleene (K3)' rather than listing K3 and Strong K3 separately. Spec audit sections C, D and research item 1.

**Blocked by:** 04, 05, 06, 07

**Status:** done

- [x] README, CONTEXT.md and ADR-0005 amended consistently, with no statement left that contradicts the final design
- [x] The three .tmp-derived claims shown wrong by the audit appear nowhere in README, CONTEXT.md or the ADRs
- [x] CLAUDE.md architecture summary matches the final operator set

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Added a README section and CONTEXT.md terms for Strong Kleene connectives versus external operators (`COALESCE` and the four inspections), the information order beside the truth order, and the note that `Project` and `Collapse` are TruthWeaver terms and `Decision` methods. ADR-0005 gains amendments 18-22 (external operators, Project/Collapse terms, `PARITY`, `If` strongest-extension rationale citing the metastability-containing multiplexer result, `BETWEEN` needs `min <= max`). CLAUDE.md architecture summary now lists the final operator set. The `PARITY` rename, `Decision.Collapse`/`Decision.Project` and `BETWEEN` bounds were already stated in README and CONTEXT.md; a grep found none of the audit-wrong claims (consensus valid in K3, `All()` false when `T<n`, `If(IsUnknown(A), B, A)` as an inverse of coalesce) in README, CONTEXT.md or the ADRs. Docs only; no code changed.
