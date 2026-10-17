# 18: A rule that combines `let` with a quantifier throws instead of compiling

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** done

**What to build:** Make definition expansion handle quantifier nodes: recurse into the quantifier body the same way it already does for `If` and inspection nodes, so `let` definitions are visible inside a `FORALL` or `EXISTS` body. `Compile` must never throw for an authoring error, so the result is either a compiled rule or a diagnostic.

**Evidence:** The audit in ticket 13 found that the node switch in DefinitionExpander has no case for a quantifier node, so it reaches its unhandled-node guard. Minimal repro: `let y = elementTrue; FORALL(x IN items, elementTrue)` against a registry with an `items` collection (same shape as the registry in QuantifierFormsTests). No existing test covers `let` with a quantifier. The ADR-0008 definition guardrails (depth, cycles, parameters) and ADR-0010 element scope may interact, so confirm what a definition may reference inside a body before choosing the behavior; if the two ADRs disagree, stop and report it.

- [ ] A failing test (rule text, JSON, YAML and builder forms) reproduces the throw before the fix and passes after
- [ ] A definition used inside a quantifier body evaluates correctly, and a definition that references the loop variable gives a clear diagnostic or works as ADR-0010 says
- [ ] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
