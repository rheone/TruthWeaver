# 22 - Definitions not reachable from the rule body are never validated

Status: needs-triage
Severity: medium
Lens: F3 Compilation
Verdict: PLAUSIBLE
Blocked by: none

## Finding F3.4: Definitions not reachable from the rule body are never validated: cycles, unknown uses and operator errors inside them compile silently

Location: `src/TruthWeaver/Compilation/DefinitionExpander.cs:62` (PLAUSIBLE, medium)

Expand only walks document.Body. A definition body is checked only when a reachable use expands it. docs/rule-text.md:102-103 states unconditionally that 'A definition cannot reference itself, directly or through a chain of other definitions', and ADR-0008 decision 6 lists cycles and unknown definitions as compile errors. A document whose unused definitions are cyclic or broken still compiles, and the problem appears only when a later edit first uses the definition. With the default Lints = None, the author gets no message at all.

### Failure scenario

`let a = b; let b = a; let c = AND(x); isAdmin` compiles successfully with no TRE0036, TRE0006 or other diagnostic. Changing the body to `isAdmin AND a` then fails with a cycle that existed all along.

### Expected behavior

Report the cycle and unknown-definition guardrails for every declared definition, whether or not it is used, or document that only definitions reachable from the rule are checked.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
