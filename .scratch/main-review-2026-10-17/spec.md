# Main review, 2026-10-17

Whole-tree review of `main` at `ef26bd5` (after merging `roadmap-tickets`), run as three read-only agents: Standards, ADR conformance (Spec) and a coverage survey. Findings were not re-verified by a human; each ticket says what to confirm first. CSharpier, `dotnet format` and Roslynator were already green.

Statuses: `ready-for-agent` is mechanical or has a clear fix in an ADR. `needs-owner-decision` has two or more reasonable answers. `needs-triage` is a lead to confirm before work.

| # | Ticket | Blocked by | Status |
| --- | --- | --- | --- |
| [01](issues/01-quantifier-memo-scope-uses-variable-name.md) | Quantifier memo scope keys on the variable, not the binding | None | done |
| [02](issues/02-selector-materialized-once.md) | Collection selector is materialized once per binding and outer element | 01 | done |
| [03](issues/03-adr-0010-conformance-gaps.md) | ADR-0010 conformance gaps | None | done (triage only) |
| [04](issues/04-tre0016-reuse.md) | TRE0016 reused although ADR-0005 retired it | None | needs-owner-decision |
| [05](issues/05-lenient-unknown-predicate-no-fault.md) | Lenient unknown predicate records no Fault | None | needs-owner-decision |
| [06](issues/06-decision-trace-optionality.md) | Decision.Trace is documented as optional but always set | None | needs-owner-decision |
| [07](issues/07-trace-names-variable-outcome.md) | Trace does not name the variable-resolution outcome | None | needs-owner-decision |
| [08](issues/08-shipped-surface-without-adr.md) | Shipped surface with no ADR | None | needs-owner-decision |
| [09](issues/09-em-dashes-in-src-comments.md) | Em dashes in src comments and XML docs | None | ready-for-agent |
| [10](issues/10-ticket-references-in-xml-docs.md) | Ticket and .scratch references in XML docs | 09 | ready-for-agent |
| [11](issues/11-operator-tables-repeated.md) | Operator symbol and label tables repeated in several files | 09 | needs-owner-decision |
| [12](issues/12-compiledrule-size.md) | CompiledRule<TContext> is 1748 lines | 09, 10, 11 | needs-owner-decision |
| [13](issues/13-audit-reachable-throws.md) | Audit reachable throw sites in the compile pipeline | None | needs-triage |
| [14](issues/14-coverage-abstractions-public-api.md) | Coverage of the Abstractions public API | None | done |
| [15](issues/15-coverage-readers.md) | Coverage of the tree-format and decision-JSON readers | None | done |
| [16](issues/16-coverage-evaluator-analysis.md) | Coverage of evaluator, analysis, definitions and printer branches | None | done |
| [17](issues/17-coverage-testing-toolcore-generators.md) | Coverage of Testing, Tool.Core and Generators | None | done |
