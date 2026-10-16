# 11-1: ADR for definitions and parameters

**Parent:** [11](11-named-sub-rules-and-parameters.md)

**What to build:** A new ADR that records the definitions model before any grammar change: the `let` keyword and rule-text grammar, JSON and YAML `definitions` section, builder definition API intent, compile-time expansion, parameter substitution (literals or sub-expressions, no loops, no recursion), guardrails (cycles, names, reserved words, predicate names, `T`/`F`/`U`, argument count, `CompilerOptions.MaxDefinitionDepth`, node and depth counting), expanded printing, and diagnostic spans. It amends the ADR-0005 operator and notation list in place, dated. Follow the parent ticket's scope; do not decide anything the parent leaves open (list it as a question for the owner instead).

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] The ADR exists under `docs/adr/`, is numbered next in sequence, and states context, decision and consequences
- [ ] ADR-0005's operator and notation list is amended in place with a dated note pointing at the new ADR
- [ ] `CONTEXT.md` and `docs/glossary.md` define **definition** and **parameter** if the ADR introduces them
- [ ] No library code changes
