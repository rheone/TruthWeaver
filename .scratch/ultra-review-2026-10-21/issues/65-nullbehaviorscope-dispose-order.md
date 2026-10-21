# 65 - Disposing NullBehaviorScope instances out of order can restore a disposed scope

Status: needs-triage
Severity: low
Lens: F7 Predicates B
Verdict: PLAUSIBLE
Blocked by: none

## Finding F7.7: Disposing scopes out of order can bring back a disposed NullBehaviorScope

Location: `src/TruthWeaver.Predicates/NullBehaviorScope.cs:81` (PLAUSIBLE, low)

Dispose sets Current.Value to this.parent without checking that this scope is the current one. If an outer scope is disposed before an inner one, disposing the inner scope later restores the outer scope, which is already disposed, as the ambient default.

### Failure scenario

`var a = NullBehaviorScope.Begin(NullBehavior.False); var b = NullBehaviorScope.Begin(PredicateFamily.Type, NullBehavior.Unknown); a.Dispose(); b.Dispose();` Both scopes are now disposed, but Current is `a`. Every later factory call on that flow resolves Default to False, not to the documented no-scope default, Unknown.

### Expected behavior

Dispose only restores the parent when this scope is Current.Value, or unwinds past disposed ancestors. An out-of-order dispose should not leave a disposed scope in effect.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
