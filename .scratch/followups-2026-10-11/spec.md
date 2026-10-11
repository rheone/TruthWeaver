# Follow-ups from the 2026-10-11 grilling

**Status:** grilled 2026-10-11, ready for implementation

Source: the open decisions after the `api-consistency-and-printers` run. Packages are unpublished, so breaking changes are allowed and recorded in `CHANGELOG.md`.

## Decisions

| Topic | Decision |
| --- | --- |
| Null answer of the ordering predicates | Keep the current state: all four ordering predicates (`IsGreaterThan`, `IsGreaterThanOrEqualTo`, `IsLessThan`, `IsLessThanOrEqualTo`) are positives. Null is `Unknown` by default and `False` under `NullBehavior.False`. Their `IsNot…` twins are the Kleene complements. `False` is not the default, because it breaks fail-closed under `NOT` and information monotonicity. |
| Where a host sets null handling once | Add a registry-wide default null behavior, so a host that wants SQL-style `False` sets it once for the whole catalog or per family. `Unknown` stays the default. |
| Constant style default | `Letters` (T, F, U) is the default for the equation printers only. Rule text, plain text and Mermaid keep `Word`. |
| Letters in rule text | The parser accepts `T`, `F` and `U` (any case) as constants, so `ConstantStyle.Letters` round-trips. Those three names become reserved: registering a predicate or naming a term `T`, `F` or `U` is an error. `LogicSymbols` (⊤, ⊥, U) is not accepted by the parser. |
| Typst names | Check the dialect's symbol names against the official Typst reference and correct them. |
| CHANGELOG encoding | Fix the stray 0x85 byte and add a check that text files are valid UTF-8. |
| Prose pass and docs plan | The prose pass over the rewrite and printer pages moves into the on-hold API documentation plan. The plan also gets: a short README per package except the main package (full root README), a later DocFX spike, and strong-k3 folded into the main structure. |

## Tickets

| Ticket | Change | Blocked by | Status |
| --- | --- | --- | --- |
| [A](issues/A-registry-wide-default-null-behavior.md) | Registry-wide default null behavior | None | ready-for-agent |
| [B](issues/B-letters-default-for-equation-printers.md) | `Letters` is the default constant style for equations | None | ready-for-agent |
| [C](issues/C-parser-accepts-t-f-u-constants.md) | The parser accepts T, F and U | B | ready-for-agent |
| [D](issues/D-verify-typst-names.md) | Verify the Typst symbol names | None | ready-for-agent |
| [E](issues/E-fix-changelog-encoding.md) | Fix the CHANGELOG encoding and guard it | None | ready-for-agent |
