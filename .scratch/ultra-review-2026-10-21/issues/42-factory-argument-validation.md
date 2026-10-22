# 42 - Predicate factories and lambda registration accept null delegates, null selectors and undefined NullBehavior values, then fail on every evaluation

Status: needs-triage
Severity: low
Lens: F8 Predicate+DataSource concepts; F6 Predicates A; F7 Predicates B
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.7: Lambda registration accepts a null delegate and fails on every evaluation instead of at registration

Location: `src/TruthWeaver/Registry/PredicateRegistryBuilder.cs:34` (PLAUSIBLE, low)

Add(schema, evaluate) does not null-check schema or evaluate. A null evaluate gives a descriptor with Evaluate and ImplementationType both null. Evaluator.cs:634-636 then goes to InvokeClassBasedAsync and calls services.GetService(null!). That throws, and the throw becomes a fault on every evaluation. A null schema throws NullReferenceException instead of the documented ArgumentException.

### Failure scenario

builder.Add(schema, null!) (for example, from a factory that returns null) builds without error. Every term of that predicate evaluates Unknown with an ArgumentNullException fault. The programming error never surfaces at startup.

### Expected behavior

ArgumentNullException.ThrowIfNull(schema) and ThrowIfNull(evaluate) in Add, like AddCollection does.

### Verification

not verified (medium or low)

## Finding F6.4: Factories do not reject a null selector at registration; every evaluation faults instead

Location: `src/TruthWeaver.Predicates/StringPredicates.cs:753` (PLAUSIBLE, low)

None of the Numeric, String or Collection factories calls `ArgumentNullException.ThrowIfNull(selector)`. The only null check in the package is the one on `timeProvider` (DateTimePredicates.cs:452). A null selector is captured in the delegate and throws NullReferenceException on each evaluation. The evaluator turns that into Unknown plus a Fault per ADR-0001, so a host wiring bug looks like a runtime data fault and is never a registration error.

### Failure scenario

A host builds its selector from configuration or reflection and passes `null!` to `StringPredicates.StartsWith("codeStartsWith", selector)`. Registration and compile succeed. Every `codeStartsWith(value: "A")` evaluation is Unknown with a NullReferenceException fault, and IsSatisfied fails closed for all requests. Nothing points to the registration as the cause.

### Expected behavior

Each factory throws ArgumentNullException for a null `selector` (and `name`) when it is called, as DateTimePredicates does for `timeProvider`.

### Verification

not verified (medium or low)

## Finding F7.6: Factories do not validate the NullBehavior value; an undefined value silently acts as Unknown

Location: `src/TruthWeaver.Predicates/NullBehaviorScope.cs:91` (PLAUSIBLE, low)

Resolve returns any value other than Default unchanged. ForNullAsync (PredicateResult.cs:31) then treats every value except False as Unknown. NullBehaviorScope.Begin rejects bad values, but the factory parameter that the doc says 'always wins' gets no check at all.

### Failure scenario

A host passes `(NullBehavior)3`, for example a value cast from configuration and mistyped. The intent was False, but registration succeeds and every null selection answers Unknown, with no error at registration time.

### Expected behavior

Resolve, or each factory, throws ArgumentOutOfRangeException for a value that is not Unknown, False or Default. This matches the validation in NullBehaviorScope.Open.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
