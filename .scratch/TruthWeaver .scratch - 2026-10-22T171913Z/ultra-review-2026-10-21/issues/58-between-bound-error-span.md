# 58 - BETWEEN bound errors point at the whole node instead of its min or max member

Status: needs-triage
Severity: low
Lens: F3 Compilation
Verdict: PLAUSIBLE
Blocked by: none

## Finding F3.7: BETWEEN bound errors point at the whole node instead of its min/max member

Location: `src/TruthWeaver/Compilation/RuleNodeCompiler.cs:683` (PLAUSIBLE, low)

An InvalidThresholdValue for BETWEEN uses `path: node.Path`, but the threshold family points at its 'k' member (line 631, PathOf(node, "k")). For JSON and YAML, LocateInJson therefore gives the span of the whole between object, not the offending bound.

### Failure scenario

`{"op":"between","min":3,"max":1,"operands":[...]}` reports TRE0008 with the span of the whole object, while `{"op":"atLeast","k":9,...}` reports the span of the `k` value.

### Expected behavior

Point at `min` or `max`, whichever violates the rule (for example `max` when max > n or min > max), the same way threshold errors point at `k`.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
