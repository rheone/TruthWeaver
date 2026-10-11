# Feature backlog from the 2026-10-11 brainstorm

**Status:** grilled 2026-10-11. Ready tickets and spikes may start; on-hold tickets wait for explicit owner approval.

Source: a brainstorm of features a user would expect from a project this broad, checked against the repository on 2026-10-11 (none of these exist unless noted). 25 candidates were considered.

## Standing rule

Before creating a new package or project, scoping one, or adding a heavy dependency to an existing package, ask the owner for confirmation. Tickets that do this start with a scope-confirmation step.

## Decisions

| Item | Decision |
| --- | --- |
| 5 localized diagnostics, 10 decision caching, 24 other logics, 25 weighted logic | Dropped. Reasons: rarely asked of a rules library; hosts can cache themselves; the project is Strong K3; a different product. No tickets. |
| 1, 2, 4, 6, 13 | Ready. |
| 16, 17a, 18, 20 | Ready, each starting with a scope confirmation. New projects: `TruthWeaver.Tool` and `TruthWeaver.Tool.Tests` (16, 20), `TruthWeaver.AspNetCore` and `TruthWeaver.AspNetCore.Tests` (18). |
| 18 scope | Authorization handler and policy builder with resource-based authorization; shipped `AuthorizationContext<TResource>` plus a custom mapper; fail closed with the decision kept; an opt-in to allow `Unknown`. |
| 21, 22 | Spike tickets first (design notes). A grilling session on each note follows. |
| 3, 7, 8, 9, 11, 12, 14, 15, 17b, 19, 22b, 23, t1 | On hold. |
| 23 | On hold in the backlog with notes: semantics fit SQL's NULL logic, predicates are opaque, `Expression.Compile` conflicts with the AOT goal, so a separate package is the recommended design. |

## Tickets

Ready: [01](issues/01-counterfactual-what-would-make-this-true.md), [02](issues/02-serializable-decision-record.md), [04](issues/04-rule-coverage-over-a-corpus.md), [06](issues/06-batch-evaluation.md), [13](issues/13-compare-rules-looser-or-tighter.md), [16](issues/16-truthweaver-dotnet-tool.md), [17a](issues/17a-textmate-grammar.md), [18](issues/18-aspnetcore-adapter.md), [20](issues/20-declarative-rule-tests.md).

Spikes: [21](issues/21-spike-overlap-and-conflict-analysis.md), [22](issues/22-spike-quantifiers.md).

On hold: [03](issues/03-plain-english-explanation.md), [07](issues/07-policy-sets-and-combining-algorithms.md), [08](issues/08-shared-term-results-across-rules.md), [09](issues/09-activitysource-tracing.md), [11](issues/11-named-sub-rules-and-parameters.md), [12](issues/12-rule-store-with-hot-reload.md), [14](issues/14-shadow-replay-of-recorded-contexts.md), [15](issues/15-importers-and-exporters.md), [17b](issues/17b-language-server.md), [19](issues/19-web-playground.md), [t1](issues/t1-terminal-ui-workbench.md), [22b](issues/22b-quantifiers-build.md), [23](issues/23-linq-and-sql-translation.md).

## Dependencies

- 04 starts after the signature of 06 is fixed. 20 needs 16. 21 needs 13.
- Hold items: 07 needs 18 (to learn what hosts need); 08 needs 07; 14 needs 02, 06, 13; 15 needs 16; 17b needs 17a; 19 needs 16; t1 needs 16 and 20; 22b needs the 22 spike and its grilling.
- Suggested order for the ready tickets, one bundle at a time: 06, 02, 13, 01, 04, 21 (spike), 22 (spike), 17a, 16, 20, 18.
