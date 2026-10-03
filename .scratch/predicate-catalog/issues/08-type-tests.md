# 08: Type-test predicates

**What to build:** The `Predicate` group of type tests becomes registerable: `IsGuid`, `IsNotGuid`, `IsNumeric`, `IsNotNumeric`, `IsUrl`, `IsNotUrl`, `IsString`, `IsNotString`, `IsDateTimeOffset` and `IsNotDateTimeOffset`. The selector shape (`string?` parse-based versus `object?` runtime-type) comes from the owner's answer in ticket 02. Each test needs a written definition recorded in its XML docs: which `Guid` formats `TryParse` accepts; which `NumberStyles` count as numeric (integers only, decimals, exponent, thousands separators) with `InvariantCulture` and whether a numeric-typed `object` counts; whether a URL must be absolute and which schemes are allowed; and for `IsDateTimeOffset` a defined `DateTimeStyles` or an ISO 8601 only rule. Negated members are the K3 complement of their positive form per the `NotX` answer. Each has a schema description and README coverage.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Each test has a written definition in its XML docs and is tested with accepted and rejected inputs at the format boundaries
- [ ] Parsing uses `InvariantCulture` throughout
- [ ] Negated members agree with the K3 complement of the positive form, including for `Unknown`
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, Type tests section](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
