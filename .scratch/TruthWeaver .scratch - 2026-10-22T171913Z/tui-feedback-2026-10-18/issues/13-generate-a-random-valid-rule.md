# 13: Generate a random valid rule

**What to build:** An action in the Insert or Rule menu fills the editor with a semi-random rule that compiles, so a user can try the workbench panes without writing a rule. The user picks a seed (or leaves it random), a depth and which operator families to include. The same seed gives the same rule. `TruthWeaver.Testing` already has `K3RuleGenerator`, which covers every operator and is deterministic for a seed, and Tool.Tui already references that package. Some generated rules do not compile (for example an out-of-range threshold), so the action retries until one does.

**Blocked by:** 05

**Status:** done

- [x] The action replaces the rule (after the same confirmation as Open or Paste when the rule has unsaved edits) with a rule that compiles.
- [x] The same seed, depth and operator families give the same rule text.
- [x] The seed is shown, so a user can repeat or share a rule.
- [x] The action never throws when a generated rule fails to compile; it retries a bounded number of times and then says so.
- [x] A test checks that 100 seeds each produce a compiling rule.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
- [x] The generated rules use the whole logic surface, not only `AND`/`OR`/`NOT`: predicate calls with arguments, the connectives, the cardinality family, `If` and the inspection operators. The operator-family choice lets the user narrow this; the default includes every family.
- [x] Terms in a generated rule are predicate names from the predicate catalog with valid sample arguments, so the generated rule reads like a real rule. If `K3RuleGenerator` only makes plain terms, extend it or wrap it; the choice is the implementer's.
- [x] A test checks that 100 default-option rules together contain every operator family at least once.
