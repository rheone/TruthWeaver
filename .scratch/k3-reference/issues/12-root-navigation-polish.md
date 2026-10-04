# 12: Root navigation and terminology polish

**What to build:** A final pass over the whole reference: root README overview and navigation finalised, category indexes complete (no pending placeholders for written operations), terminology and notation consistent across documents, the repo README and CONTEXT.md link to the reference, and the validation report's findings addressed or recorded.

**Blocked by:** 11

**Status:** done

- [ ] All pending placeholders in indexes are resolved except the predicates hold
- [ ] The repo README and CONTEXT.md link to docs/strong-k3/
- [ ] The harness passes and the validation report is current
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: Phases 7 and 8 of the brief. See also [spec](../spec.md).

## Comments

- 2026-10-03: Root `docs/strong-k3/README.md`: replaced the "being added / pending" note (no index has a pending entry) and relabelled the navigation row "Derived Logical Operations". Added links to the reference from the repository `README.md` (Design documents) and `CONTEXT.md` (Related documents). Refreshed `VALIDATION.md` (U6 resolved, links noted). Deliberately not done, awaiting the owner: U3 (`CONTEXT.md` "never called a gate" wording) and U7 (folding in and deleting PROPOSAL.md, which other pages and the checker still cite); U1, U2, U4, U5, U8 to U12 unchanged.
