# Predicate catalog: K3 gap list

Gap list of the predicates named in the **Final Semantic Inventory** of
[`.scratch/2026-10-02-TODO.md`](../2026-10-02-TODO.md) against what
`TruthWeaver.Predicates` and `TruthWeaver.Testing` provide today. It is the hand-off for the
predicate-catalog track (see [issue 01](issues/01-brainstorm-general-use-predicates-and-literal-kinds.md))
from k3-conformance ticket 30. **No predicate is implemented by this document.**

## Method

Verified by reading the source on branch `StrongK3+Operations` (after k3-conformance tickets 01-29):

- `src/TruthWeaver.Predicates/StringPredicates.cs`, `RegexPredicates.cs`, `CollectionPredicates.cs`,
  `ResolvedValuePredicates.cs`, `PredicateResult.cs`
- `src/TruthWeaver.Testing/FakePredicates.cs`
- `src/TruthWeaver.Abstractions/{LiteralKind,PredicateArguments,IPredicate}.cs`
- [CONTEXT.md](../../CONTEXT.md), [ADR-0001](../../docs/adr/0001-kleene-failure-model.md),
  [ADR-0003](../../docs/adr/0003-rule-syntax-and-serialization.md),
  [ADR-0005](../../docs/adr/0005-strong-k3-language-surface.md)

Status vocabulary: **present** (same name, same meaning), **present-under-another-name**, **missing**.

## What the catalog provides today

Every catalog member is a selector-parameterized factory returning a tuple
`(PredicateSchema Schema, Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate)`.
The host supplies `Func<TContext, T?> selector` at registration; rule text supplies literal arguments.

| Member | Class | Selector | Arguments (`LiteralKind`) |
| --- | --- | --- | --- |
| `Equals` | `StringPredicates` | `string?` | `value` (`String`) |
| `EqualsIgnoreCase` | `StringPredicates` | `string?` | `value` (`String`) |
| `EqualsConfigurable` | `StringPredicates` | `string?` | `value` (`String`), `ignoreCase` (`Boolean`, default `true`), `trim` (`Boolean`, default `false`) |
| `StartsWith` | `StringPredicates` | `string?` | `value` (`String`) |
| `EndsWith` | `StringPredicates` | `string?` | `value` (`String`) |
| `Contains` | `StringPredicates` | `string?` | `value` (`String`) |
| `IsNullOrEmpty` | `StringPredicates` | `string?` | none |
| `Matches` | `RegexPredicates` | `string?` | `pattern` (`String`) |
| `SetEquals` | `CollectionPredicates` | `IReadOnlyCollection<string>?` | `values` (`StringArray`) |
| `Create<TContext, TResolved>` / `Create<TContext>` | `ResolvedValuePredicates` | host-defined | host-defined (plumbing, not a predicate) |

`TruthWeaver.Testing.FakePredicates` provides only test doubles (`Returning` with `bool` or `TruthValue`,
`Faulting`, `Scripted`); it contributes no inventory predicate.

### Conventions observed in the existing members

- **Return type.** The delegate type is already `ValueTask<TruthValue>`, so a predicate *can* return
  `Unknown`. In practice **no catalog member ever does**: all of them go through the internal
  `PredicateResult.FromBoolAsync(bool)`, so the catalog is effectively two-valued. `Unknown` appears only
  when a predicate faults (invalid regex) per ADR-0001.
- **Null selected value.** Every string and regex member returns `False` for a `null` selection;
  `SetEquals` treats a `null` collection as empty; `IsNullOrEmpty` returns `True` for `null` (its
  definition). Nothing returns `Unknown` for null. See [Open questions](#open-questions-for-the-repo-owner).
- **Comparison.** Ordinal and case-sensitive by default. `EqualsIgnoreCase` is ordinal-ignore-case. `EqualsConfigurable` is
  also ordinal (k3-followups 12); its `culture` argument was removed (k3-followups 20).
- **Naming.** Members are named like BCL methods (`Equals`, `StartsWith`); the inventory uses `Equal`,
  `IsEmpty`, etc. The registered predicate `name` is chosen by the host at registration, so the member name
  is the factory name, not the rule-text name.
- **Literal kinds.** `LiteralKind` is the closed set `String`, `Int64`, `Decimal`, `Boolean`,
  `DateTimeOffset`, `Guid` plus the matching `*Array` forms. There is no `DateTime`, `TimeSpan`, `Uri` or
  "any value" kind.

## Summary

| Inventory section | Items | Present | Present under another name | Missing |
| --- | ---: | ---: | ---: | ---: |
| Primitive types | 10 | 10 | 0 | 0 |
| Numeric | 4 | 4 | 0 | 0 |
| String | 13 | 12 | 1 | 0 |
| Collection | 10 | 10 | 0 | 0 |
| DateTimeOffset | 8 | 5 | 0 | 3 |
| Type tests (`Predicate`) | 10 | 10 | 0 | 0 |
| **Total** | **55** | **51** | **1** | **3** |

Ordering and range members are defined for `Int64` and `Decimal` only. For `Boolean` and `Guid` they are not defined, and for `DateTimeOffset` they are the date-time comparison predicates. `IsEmpty` also ships an `IsNotEmpty` twin. The three `DateTime` overloads of `After`, `Before` and `Between` are not added: the host converts to `DateTimeOffset` in the selector.

Present members that are **not** in the inventory: `StringPredicates.EqualsIgnoreCase` (covered by the
inventory's `ignoreCase` option) and `CollectionPredicates.SetEquals` (no inventory counterpart; the
inventory's collection `In`/`Contains` are different operations).

## Primitive types

Inventory: `Equal`, `NotEqual`, `LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`,
`IsDefault`, `IsNotDefault`, `IsNull`, `IsNotNull`.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `Equal` | present | `NumericPredicates.Equal` (`Int64`, `Decimal`), `ScalarPredicates.Equal` (`Boolean`, `Guid`, `DateTimeOffset`) | Needs one thin public overload per scalar kind over a shared generic helper. `Decimal` vs `Int64` comparison semantics (cross-kind promotion) must be decided. |
| `NotEqual` | present | `NumericPredicates.NotEqual` (`Int64`, `Decimal`), `ScalarPredicates.NotEqual` (`Boolean`, `Guid`, `DateTimeOffset`) | As `Equal`. Null input: see open question 1. |
| `LessThan` | present | `NumericPredicates.LessThan` (`Int64`, `Decimal`) | Ordering uses `CompareTo` on the value type, no culture dimension. `Boolean` and `Guid` ordering is arguably meaningless; decide whether the ordering family is limited to `Int64`, `Decimal`, `DateTimeOffset`. |
| `GreaterThan` | present | `NumericPredicates.GreaterThan` (`Int64`, `Decimal`) | As `LessThan`. |
| `LessThanOrEqual` | present | `NumericPredicates.LessThanOrEqual` (`Int64`, `Decimal`) | As `LessThan`. |
| `GreaterThanOrEqual` | present | `NumericPredicates.GreaterThanOrEqual` (`Int64`, `Decimal`) | As `LessThan`. |
| `IsDefault` | present | `NumericPredicates.IsDefault` (`Int64`, `Decimal`), `ScalarPredicates.IsDefault` (`Boolean`, `Guid`, `DateTimeOffset`) | Needs a generic `TValue` selector and no literal argument, so no new `LiteralKind`. `default(T)` for a reference type is `null`, which overlaps `IsNull`; decide whether `IsDefault` is value-type-only. |
| `IsNotDefault` | present | `NumericPredicates.IsNotDefault` (`Int64`, `Decimal`), `ScalarPredicates.IsNotDefault` (`Boolean`, `Guid`, `DateTimeOffset`) | As `IsDefault`. |
| `IsNull` | present | `NumericPredicates.IsNull` (`Int64`, `Decimal`), `ScalarPredicates.IsNull` (`Boolean`, `Guid`, `DateTimeOffset`) | Needs a generic nullable selector. Input being `null` is the *subject* of the test, so the result is `True`, never `Unknown`; this is the one family where the null convention inverts. |
| `IsNotNull` | present | `NumericPredicates.IsNotNull` (`Int64`, `Decimal`), `ScalarPredicates.IsNotNull` (`Boolean`, `Guid`, `DateTimeOffset`) | As `IsNull`. |

## Numeric

Inventory: `Between(value, n, k)`, `Outside(value, n, k)`, `In(value, candidate1, ...)`,
`NotIn(value, candidate1, ...)`.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `Between` | present | `NumericPredicates.Between` (`Int64`, `Decimal`) | Arguments `n`, `k` as `Int64` or `Decimal`. Bounds are inclusive on both ends. Reversed bounds (`n > k`) are an authoring error: a compile-time diagnostic for literal bounds, an argument error otherwise. |
| `Outside` | present | `NumericPredicates.Outside` (`Int64`, `Decimal`) | Exact complement of `Between` and its registered twin: the K3 complement (`Unknown` stays `Unknown`), not a boolean negation. |
| `In` | present | `NumericPredicates.In`, `ScalarPredicates.In` | Scalar membership only. Candidates map to `Int64Array` / `DecimalArray`; the inventory's variadic form is one array argument in the predicate schema. |
| `NotIn` | present | `NumericPredicates.NotIn`, `ScalarPredicates.NotIn` | As `In`; the registered twin of `In`. K3 caveat: SQL-style `NOT IN` with a null candidate is `Unknown`; candidates here are literals and cannot be null, so there is no such case. |

## String

Inventory: each predicate "with Trim, Culture, ignoreCase".

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `Contains` | present | `StringPredicates.Contains` | Ordinal, case-sensitive, no `Trim`/`Culture`/`ignoreCase` arguments. Options are missing, not the predicate. |
| `EndsWith` | present | `StringPredicates.EndsWith` | As `Contains`. |
| `StartsWith` | present | `StringPredicates.StartsWith` | As `Contains`. |
| `Equal` | present-under-another-name | `StringPredicates.Equals` (exact), `EqualsIgnoreCase`, `EqualsConfigurable` | `EqualsConfigurable` is the only member with `ignoreCase` and `trim` arguments (defaults `true`, `false`). It is ordinal and has no `culture` argument (k3-followups 12 and 20, open question 3). |
| `IsNullOrEmpty` | present | `StringPredicates.IsNullOrEmpty` | No arguments. `Trim` is meaningless here and `ignoreCase`/`Culture` do not apply. Returns `True` for `null`. |
| `Matches` | present | `RegexPredicates.Matches` | `pattern` only; `RegexOptions.None`, 1-second timeout, cached per pattern. `ignoreCase` would map to `RegexOptions.IgnoreCase` (a new optional `Boolean` argument); `Culture` maps to `CultureInvariant`; `Trim` is questionable for a regex. |
| `IsEmpty` | present | `StringPredicates.IsEmpty` (twin `IsNotEmpty` | Non-null empty string. Null input: `False` or `Unknown`, see open question 1. |
| `IsNotNullOrEmpty` | present | `StringPredicates.IsNotNullOrEmpty` | Negation of `IsNullOrEmpty`; returns `False` for `null`. |
| `IsNullOrWhiteSpace` | present | `StringPredicates.IsNullOrWhiteSpace` | `Trim` is implied. Listed in issue 01 as a candidate. |
| `IsNotNullOrWhiteSpace` | present | `StringPredicates.IsNotNullOrWhiteSpace` | Negation of `IsNullOrWhiteSpace`. |
| `NotContains` | present | `StringPredicates.NotContains` | K3 complement of `Contains`. If null selects `False` for `Contains`, `NotContains` of null must not be `True` by accident; decide per open question 1. |
| `NotEqual` | present | `StringPredicates.NotEqual` | As `NotContains`, against `Equal`. |
| `NotMatches` | present | `RegexPredicates.NotMatches` | As `NotContains`, against `Matches`. An invalid pattern still faults to `Unknown`. |

## Collection

Inventory: `IsEmpty`, `IsNotEmpty`, `Contains`, `In`, `NotIn`, `CountEqual`, `CountLessThan`,
`CountGreaterThan`, `CountLessThanOrEqual`, `CountGreaterThanOrEqual`.

`CollectionPredicates` provides the whole collection family (ticket 05, with `NotX` twins `IsNotEmpty`, `NotContains`, `NotContainsAny`, `NotContainsAll`, `IsNotSubsetOf`, `NotIn` and `NotCount*`) beside `SetEquals`, over `IReadOnlyCollection<string>?`. Element type is
therefore `string` only; `Int64`/`Decimal`/`Guid` collections need generic or per-kind overloads.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `IsEmpty` | present | `CollectionPredicates` | A `null` collection is treated as empty by `SetEquals`; `IsEmpty(null)` would be `True` by that convention. |
| `IsNotEmpty` | present | `CollectionPredicates` | As `IsEmpty`, so `IsNotEmpty(null)` is `False`. |
| `Contains` | present | `CollectionPredicates` | Collection contains the literal value: arguments `value` (`String`, or per-kind). Distinct from `StringPredicates.Contains`. |
| `ContainsAny` | present | `CollectionPredicates` | Added by decision 4: at least one element is in the candidate array. Twin `NotContainsAny`. |
| `ContainsAll` | present | `CollectionPredicates` | Added by decision 4: every candidate is an element. Twin `NotContainsAll`. |
| `IsSubsetOf` | present | `CollectionPredicates` | Added by decision 4: every element is in the candidate array. Twin `IsNotSubsetOf`. |
| `In` | present | `CollectionPredicates` | Resolved (question 4): `In` is scalar-only membership, so a collection selector is a compile error. The collection predicates are `ContainsAny`, `ContainsAll` and `IsSubsetOf`, each with a twin. |
| `NotIn` | present | `CollectionPredicates` | As `In`. |
| `CountEqual` | present | `CollectionPredicates` | Argument `count` (`Int64`). Null collection yields `Unknown` by default (`NullBehavior.False` is the host option). Twins `NotCount*`. |
| `CountLessThan` | present | `CollectionPredicates` | As `CountEqual`. |
| `CountGreaterThan` | present | `CollectionPredicates` | As `CountEqual`. |
| `CountLessThanOrEqual` | present | `CollectionPredicates` | As `CountEqual`. |
| `CountGreaterThanOrEqual` | present | `CollectionPredicates` | As `CountEqual`. |

## DateTimeOffset

Inventory: `AfterNow`, `BeforeNow`, `After` and `Before` (each with `DateTimeOffset` and `DateTime`
arguments), `Between` (each with `DateTimeOffset` and `DateTime` arguments).

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `AfterNow` | present | `DateTimePredicates` | Takes a required `TimeProvider` at registration and no rule-text arguments. The clock is read once per predicate evaluation; the engine memoizes per term, so there is no cross-term snapshot. Twin `NotAfterNow`. |
| `BeforeNow` | present | `DateTimePredicates` | As `AfterNow`. Twin `NotBeforeNow`. |
| `After(value, DateTimeOffset)` | present | `DateTimePredicates` | Argument kind `DateTimeOffset` already exists. Strict `>`. |
| `After(value, DateTime)` | not added (decided: host converts) | none | **Needs a new `LiteralKind`** (`DateTime`) or a documented conversion. `DateTime` has no offset and a `Kind` of `Utc`/`Local`/`Unspecified`, so conversion to `DateTimeOffset` is host-timezone-dependent for `Local`/`Unspecified`. Adding a `LiteralKind` is a closed-set extension (ADR-0003, CONTEXT.md) and a breaking change for exhaustive switches. Recommend not adding it: accept the `DateTimeOffset` literal and let the host convert. |
| `Before(value, DateTimeOffset)` | present | `DateTimePredicates` | As `After`. |
| `Before(value, DateTime)` | not added (decided: host converts) | none | As `After(value, DateTime)`. |
| `Between(value, DateTimeOffset, DateTimeOffset)` | present | `DateTimePredicates` | Inclusive on both ends; reversed bounds are an authoring error (question 7). |
| `Between(value, DateTime, DateTime)` | not added (decided: host converts) | none | As `After(value, DateTime)`. |

Overload by argument type is not expressible in a single predicate schema (one name, one schema), so the
`DateTime` overloads are either separate predicate names or are dropped in favour of the `DateTimeOffset`
form.

## Type tests (`Predicate` group)

Inventory: `IsGuid`, `IsNotGuid`, `IsNumeric`, `IsNotNumeric`, `IsUrl`, `IsNotUrl`, `IsString`, `IsNotString`,
`IsDateTimeOffset`, `IsNotDateTimeOffset`.

These imply a selector returning `object?` (or `string?` for the parse-based tests). No catalog member
takes an `object?` selector today. They belong to the per-kind static class `TypePredicates` (question 8).

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `IsGuid` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsNotGuid` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsNumeric` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsNotNumeric` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsUrl` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsNotUrl` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsString` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsNotString` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsDateTimeOffset` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |
| `IsNotDateTimeOffset` | present | `TypePredicates` (ticket 08) | Both `string?` and `object?` overloads; definitions in the XML docs. |

## Open questions for the repo owner

Resolution status: the rules are recorded in
[CONTEXT.md](../../CONTEXT.md#predicate-catalog-rules).

- Question 1 (null input): **resolved**. Existing members keep `False`; new comparison, range and count
  families return `Unknown` for null; null tests stay definite.
- Question 3 (culture): **resolved**. Ordinal only; `EqualsConfigurable` no longer has a `culture` argument (k3-followups 20).
- Question 5 (`DateTime` arguments): **resolved**. `DateTimeOffset` only.
- Question 6 (clock predicates): **resolved**. `TimeProvider` supplied at registration.
- Question 2 (`NotX` shape): **resolved**. Every positive predicate has a registered first-class `NotX`
  twin, defined as the Strong Kleene complement (`Unknown` stays `Unknown`).
- Question 4 (collection `In`/`NotIn`): **resolved**. `In`/`NotIn` are scalar-only membership and a
  collection selector is a compile error. `ContainsAny`, `ContainsAll` and `IsSubsetOf` are the
  collection predicates, each with a twin.
- Question 7 (bounds): **resolved**. `Between` is inclusive on both ends and `Outside` is its exact
  complement. Reversed bounds are an authoring error (a compile-time diagnostic for literal bounds, an
  argument error otherwise) and are never swapped silently.
- Question 8 (selector shapes): **resolved**. New families are per-kind static classes
  (`NumericPredicates`, `DateTimePredicates`, `TypePredicates`) with selectors typed for their kind.

The rules for questions 2, 4, 7 and 8 are recorded in
[CONTEXT.md](../../CONTEXT.md#predicate-catalog-rules) (ticket 09).


1. **Null input: `False` or `Unknown`?** The existing convention (every catalog XML doc and issue 01) is
   "null selected value is `False`, never a fault". Now that predicates return `TruthValue`, a missing
   value is the textbook K3 case for `Unknown` (`NULL > 5` is unknown, not false). The consequence is
   concrete: with `False`, `NOT GreaterThan(x, 5)` is `True` for a null `x`; with `Unknown` it stays
   `Unknown` and `IsSatisfied` is fail-closed. This decision also determines whether `NotX` predicates may
   be implemented as a K3 `NOT` of their positive form. Recommendation: `Unknown` for comparison, range and
   count predicates; keep `True`/`False` for the null tests themselves (`IsNull`, `IsNullOrEmpty`,
   `IsNullOrWhiteSpace`); leave the existing string members as they are unless the owner wants consistency
   (changing them is a behaviour change for current consumers).
2. **`NotX` predicates.** Whether each negated predicate is a first-class registered predicate (as the
   inventory lists) or is left to `NOT` in the rule. If first-class, define them as the K3 complement
   (`Unknown` maps to `Unknown`).
3. **Culture.** The inventory asks for a `Culture` option on every string predicate, but the catalog's
   stated stance (issue 01, `StringPredicates` XML docs) is "ordinal or invariant only, never
   culture-sensitive", and `EqualsConfigurable` already breaks it by accepting an arbitrary culture name
   (it also faults on an unknown name). Note that CONTEXT.md does not itself state a culture rule; the rule
   lives in issue 01 and the predicate XML docs, so the "CONTEXT.md no-culture-sensitive-comparison rule"
   is not actually recorded there. Options: (a) restrict `Culture` to `""` (invariant) and the ordinal
   modes, (b) allow named cultures as `EqualsConfigurable` does and document the exception, (c) drop
   `Culture` from the other predicates. Recommendation: (a), and record the rule in CONTEXT.md.
4. **Collection `In`/`NotIn` meaning.** Subset ("all elements are candidates"), intersection
   ("any element is a candidate") or scalar membership. Needs a one-line definition before implementation.
5. **`DateTime` arguments.** Add a `DateTime` `LiteralKind` (breaking, closed set) or accept only
   `DateTimeOffset`. Recommendation: accept only `DateTimeOffset`.
6. **Clock predicates.** Whether `AfterNow`/`BeforeNow` belong in the shipped catalog (with a
   `TimeProvider` registration parameter) or stay host-authored, as issue 01 and CONTEXT.md currently say.
7. **Bounds.** Inclusive vs exclusive for `Between`/`Outside` (numeric and date) and the result for
   reversed bounds.
8. **Selector shapes.** Generic `Func<TContext, TValue?>` factories with per-kind overloads versus
   per-kind classes (`NumericPredicates`, `DateTimePredicates`, `TypePredicates`); issue 01 proposed the
   latter for numeric and date/time.

## Suggested grouping for follow-on tickets

Smallest independent slices, in a sensible order (each is a catalog addition, with no engine change):

1. String completions: `IsEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace`,
   `NotContains`, `NotEqual`, `NotMatches` (blocked by questions 1-3).
2. Numeric and primitive comparison family (blocked by questions 1, 7).
3. Collection family (blocked by questions 1, 4).
4. Date/time comparison without clock predicates (blocked by questions 5, 7); clock predicates after 6.
5. Type tests (blocked by question 8 and the definitions of numeric/URL).
