# 08: Type-test predicates

**What to build:** The `Predicate` group of type tests becomes registerable: `IsGuid`, `IsNotGuid`, `IsNumeric`, `IsNotNumeric`, `IsUrl`, `IsNotUrl`, `IsString`, `IsNotString`, `IsDateTimeOffset` and `IsNotDateTimeOffset`. Each positive test and its `NotX` twin form a pair (`IsGuid`/`IsNotGuid` and so on), the twin being the K3 complement with `Unknown` staying `Unknown`. The members live in the per-kind static class `TypePredicates`. The selector shape (`string?` parse-based versus `object?` runtime-type) is not decided by the recorded rules; decide it in the ticket comments before implementing, or report it as a blocker. Each test needs a written definition recorded in its XML docs: which `Guid` formats `TryParse` accepts; which `NumberStyles` count as numeric (integers only, decimals, exponent, thousands separators) with `InvariantCulture` and whether a numeric-typed `object` counts; whether a URL must be absolute and which schemes are allowed; and for `IsDateTimeOffset` a defined `DateTimeStyles` or an ISO 8601 only rule. Negated members are the K3 complement of their positive form per the `NotX` twin rule. Each has a schema description and README coverage.

**Blocked by:** 10

**Status:** done

- [x] The failing test run is shown before the implementation
- [x] Each test has a written definition in its XML docs and is tested with accepted and rejected inputs at the format boundaries
- [x] Parsing uses `InvariantCulture` throughout
- [x] Negated members agree with the K3 complement of the positive form, including for `Unknown`
- [x] README and the gap list show the predicates as present
- [x] The full validation from CLAUDE.md passes

Source: [gap list, Type tests section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

Owner decision (2026-10-04), recorded before implementation as the ticket requires:

- **Selector shape:** both overloads. Each type test has a `string?` (parse-based) member and an `object?` (runtime-type) member under the same name. A `null` selected value is `Unknown`.
- **`IsNumeric`:** `NumberStyles.Float` with `InvariantCulture`. A sign, a decimal point and an exponent are accepted. Thousands separators and currency symbols are rejected.
- **`IsUrl`:** an absolute URI with the `http` or `https` scheme only (`Uri.TryCreate` with `UriKind.Absolute`).
- **`IsDateTimeOffset`:** ISO 8601 with an explicit offset or `Z`, `DateTimeStyles.None`, `InvariantCulture`. Text without an offset is rejected, so the instant is never guessed.

- 2026-10-04: Implemented in `src/TruthWeaver.Predicates/TypePredicates.cs` (10 names, each with `string?` and `object?` overloads, null is `Unknown`, no `nullBehavior` option). Definitions follow the owner decision above; `IsNumeric` parses a finite `double` (NaN, Infinity and overflow rejected; numeric-typed objects count except non-finite floats), `IsDateTimeOffset` needs seconds and an offset or `Z`. Tests are in `TypePredicatesTests`. The ten predicates are on the `K3PredicateDocumentationHold` list (no `docs/strong-k3/predicates/` pages yet).
