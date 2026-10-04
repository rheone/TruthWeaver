# 08: Type-test predicates

**What to build:** The `Predicate` group of type tests becomes registerable: `IsGuid`, `IsNotGuid`, `IsNumeric`, `IsNotNumeric`, `IsUrl`, `IsNotUrl`, `IsString`, `IsNotString`, `IsDateTimeOffset` and `IsNotDateTimeOffset`. Each positive test and its `NotX` twin form a pair (`IsGuid`/`IsNotGuid` and so on), the twin being the K3 complement with `Unknown` staying `Unknown`. The members live in the per-kind static class `TypePredicates`. The selector shape (`string?` parse-based versus `object?` runtime-type) is not decided by the recorded rules; decide it in the ticket comments before implementing, or report it as a blocker. Each test needs a written definition recorded in its XML docs: which `Guid` formats `TryParse` accepts; which `NumberStyles` count as numeric (integers only, decimals, exponent, thousands separators) with `InvariantCulture` and whether a numeric-typed `object` counts; whether a URL must be absolute and which schemes are allowed; and for `IsDateTimeOffset` a defined `DateTimeStyles` or an ISO 8601 only rule. Negated members are the K3 complement of their positive form per the `NotX` twin rule. Each has a schema description and README coverage.

**Blocked by:** 10

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Each test has a written definition in its XML docs and is tested with accepted and rejected inputs at the format boundaries
- [ ] Parsing uses `InvariantCulture` throughout
- [ ] Negated members agree with the K3 complement of the positive form, including for `Unknown`
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, Type tests section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
