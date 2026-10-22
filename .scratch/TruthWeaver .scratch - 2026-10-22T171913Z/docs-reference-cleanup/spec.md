# Documentation reference cleanup

## Goal

`docs/strong-k3/` is a current technical reference for the package. It covers the functionality of the package, the fundamentals of Strong Kleene (K3) logic and a reference guide to each Operation. It does not record project history, tickets, ADRs, pending decisions or developer-only detail. The same standard applies to the in-scope Markdown files of the whole repository.

## Decisions

Recorded in the planning session of 2026-10-04. The owner approved each one.

1. `PROPOSAL.md` and `VALIDATION.md` are deleted. Git history is the archive. The inventory moves to `specification/operations.md`, the classification terms to `specification/terminology.md`, and the page template and checker description to `docs/doc-examples.md`.
2. The "Implementation Notes" section on each operation page becomes "Evaluation behavior". It keeps observable behavior (short-circuit, `NotEvaluated`, `EvaluationMode.Exhaustive`, fault effects, rewrite and simplifier behavior). Developer-only detail moves to code comments in `Evaluator` and `OperatorDefinitions`.
3. `predicates/README.md` stays as a placeholder, reworded in present tense without "on hold" or ticket language.
4. New pages: `specification/syntax.md` (precedence, mixing rule, call and infix forms, symbols, case, JSON and YAML shape, stated once), `specification/diagnostics.md` (each `TRE` code once: name, cause, fix; no quoted message text), `specification/evaluation.md` (expression and `Decision`, modes, short-circuit, faults, fail-closed `IsSatisfied`), `specification/operations.md` (the inventory). Operation pages keep only behavior specific to the Operation and link to these pages.
5. Mermaid diagrams: 10 in total. Keep the 7 existing ones (2 in `values.md`, and `XOR`, `EQUIVALENT`, `If`, `Project`, `Collapse`). Add the operation map, the evaluation flow and the cardinality interval.
6. Category names and directories do not change. Operation page headings use sentence case. The `Category:` and `Category index:` lines stay.
7. Glossaries. The project-wide glossary is `docs/glossary.md`, linked from the root `README.md`. It covers new and confusing vocabulary for a reader of the project, including terms outside the K3 reference. `specification/terminology.md` is the K3 glossary and holds K3-specific concepts only.
8. Link direction. K3 pages link only to other K3 pages. They never link to `README.md`, `CONTEXT.md`, `docs/doc-examples.md`, ADRs, `.scratch` or `CHANGELOG.md`. The root `README.md` may link into the K3 docs.
9. Documentation standard scope: every root `*.md`, every `README.md` outside `.claude/`, and every file they link to recursively. Recursion stops at `docs/adr/**`, `.scratch/**`, `CHANGELOG.md` and `.agents/**`. A dev can opt other files in with a marker comment. The standard is written into `CLAUDE.md` (about 25 lines) and enforced by a lint test.
10. The lint test starts with a baseline list of in-scope files that are not yet cleaned. The list shrinks as tickets land.
11. Threshold arity needs no new work. [k3-followups 36](../k3-followups/issues/36-threshold-family-accepts-one-operand.md) is done: the table minimum is 1. The documents state "1 or more operands".
12. The root `README.md` breakdown is a second pass. Ticket 12 audited and planned it, and the owner approved the plan on 2026-10-04. Tickets 13 to 24 carry it out. See [the plan](readme-breakdown-plan.md).

## Related tickets

- [k3-reference 14](../k3-reference/issues/14-keep-reference-in-sync.md): keep the reference in sync with `OperatorDefinitions`. Unchanged by this effort.
- [k3-reference 15](../k3-reference/issues/15-reference-conventions.md): reference conventions. Unchanged by this effort.
- [k3-reference 16](../k3-reference/issues/16-validate-and-render-mermaid-diagrams.md): render the diagrams. Needs the Mermaid connector authorized. Ticket 11 here checks syntax only.

## Out of scope

- Editing `README.md`, `CONTEXT.md`, `docs/data-sources.md`, `docs/agents/*` and `AGENTS.md` beyond the link fixes the other tickets need.
- Changing operator semantics, the engine or the operator table.
- Documenting individual predicates.
