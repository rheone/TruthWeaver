# Predicate catalog track

- [Issue 01: brainstorm of general-use predicates and literal kinds](issues/01-brainstorm-general-use-predicates-and-literal-kinds.md)
- [K3 gap list](k3-gap-list.md): the TODO's Final Semantic Inventory predicates marked present or
  missing against `TruthWeaver.Predicates`. Closed: every predicate is present and every question is
  resolved (k3-conformance ticket 30).

## Acceptance criterion for every predicate ticket

Every ticket that adds, renames or removes a predicate carries this criterion: the change adds, renames or removes the reference document `docs/strong-k3/predicates/<kind>-<factory>.md`, updates the category index and the root navigation, and removes the predicate from `K3PredicateDocumentationHold` when it is documented. The document follows the Operation template and states the null-selected-value behavior and the Unknown cases. The rule is enforced by `K3ReferenceSyncChecker` (reference ticket 14).
