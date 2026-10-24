# 60 - The EQUIVALENT description calls XNOR formerly, but XNOR is a current alias

Status: needs-triage
Severity: low
Lens: F1 Ast+Abstractions
Verdict: PLAUSIBLE
Blocked by: none

## Finding F1.5: EQUIVALENT description calls XNOR 'formerly', but XNOR is a current alias

Location: `src/TruthWeaver/Ast/OperatorDefinitions.cs:55` (PLAUSIBLE, low)

The description that OperatorInfo returns publicly reads 'Logical biconditional (IFF, formerly XNOR) ...'. docs/strong-k3/specification/operations.md lists 'Aliases IFF and XNOR', and the DSL and tree formats still accept XNOR. The documentation standard says reference text describes current behavior, not history.

### Failure scenario

A rule-authoring UI shows the description for an EQUIVALENT node, and a reader concludes that XNOR is no longer accepted, which is wrong.

### Expected behavior

The description names IFF and XNOR as aliases, with no 'formerly'.

### Verification

not verified (medium or low)

Verified 2026-10-24: reproduced - src/TruthWeaver/Ast/OperatorDefinitions.cs:55 reads "Logical biconditional (IFF, formerly XNOR) ...", while docs/strong-k3/specification/operations.md:15 lists "Aliases `IFF` and `XNOR`" and both the DSL grammar (docs/rule-text.md:33, `infix_op` includes `"XNOR"`) and `TreeFormatOpNames.BuildReadTable` (src/TruthWeaver/Ast/TreeFormatOpNames.cs:57, `table["xnor"] = "Equivalent"`) still accept XNOR as live input.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
