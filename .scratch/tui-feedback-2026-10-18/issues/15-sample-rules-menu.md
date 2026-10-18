# 15: Sample rules menu

**What to build:** A menu offers sample rules that show what the logic can do. Together the samples cover predicates with arguments, the connectives (`AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `PARITY`), the cardinality family (`AtLeast`, `AtMost`, `Exactly`, `ExactlyOne`, `ANY`, `ALL`, `NONE`, `BETWEEN`), `If`, the inspection operators (`IsTrue`, `IsUnknown`, `COALESCE`), `let` definitions, quantifiers, `from(...)` variables and notes. Each sample has a name and a one-line description of what it shows.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] At least one sample per family listed above, each compiling in the workbench's permissive mode.
- [ ] Choosing a sample replaces the rule after the same confirmation as Open when there are unsaved edits.
- [ ] A test compiles every sample and fails when a family has none.
- [ ] Samples are rule text and carry notes that explain the logic.
- [ ] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
