# 04: Skeleton and navigation

**What to build:** The approved directory tree with an index README.md in every meaningful directory and the root README.md giving an overview and navigation to the specification, values, semantics, Gates / Operators, Predicates, Functions, Cardinality Functions, Derived Operations and Result Transformations. Indexes link to operations without duplicating their definitions. The predicates index is a placeholder stating predicate documentation is on hold until the predicates are implemented (see ticket 13).

**Blocked by:** 01

**Status:** done

- [x] Every directory in the approved tree has an index with relative links to its members (links to not-yet-written operation files may be listed as pending until their tickets land)
- [x] Root README navigation reaches every category
- [x] The harness link check passes
- [x] No substantive operation definition is duplicated in an index
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: Phases 3 and 7 of the brief. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added the approved tree's indexes under docs/strong-k3: root README.md (overview and a navigation table reaching specification, gates, derived, cardinality, functions, result transformations and predicates) and a README.md in each of those seven directories. Operations that have no document yet are listed as plain text marked "(pending)" with a one-line role summary and no definition, so no link dangles; ticket 05 onward turns each into a link when its document lands. The predicates index is a placeholder stating the hold. The specification index lists values, semantics, terminology and notation as pending (ticket 03). No Mermaid diagram was added because none materially helps navigation (the root table does the job; PROPOSAL.md keeps the structure diagram). The harness (`K3ReferenceTests`, 16 tests, includes the real-tree check) passes on the skeleton: all relative links and anchors resolve, and indexes are not treated as operation documents. Category label wording follows the ticket 02 comment ("Derived Operations" is the navigation name; the per-document `Category:` label the harness expects is "Derived Logical Operations").
