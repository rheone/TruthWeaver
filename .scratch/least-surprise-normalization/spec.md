# Least-surprise normalization

**Status:** grilled 2026-10-10, ready for implementation

Source: a three-area audit (public API and options; language surface and predicates; rewrites, testing package, printers, docs and tooling) of conventions that would surprise a rule author, a library user or a contributor. The top findings were checked against the code. Items 3, 13, 14 and 15 of the ranked list rest on a code reading that tickets 03, 14 and 15 confirm with a failing test first.

The packages are unpublished (`1.0.0-dev`), so breaking changes are allowed. Each one is recorded in `CHANGELOG.md` with a migration step.

## Decisions

| Finding | Decision |
| --- | --- |
| DSL threshold `k` that does not parse becomes 0 | Compile error, one shared integer-bound parser |
| Date-time literal with no offset uses the host zone | Require `Z` or an offset |
| Tree formats ignore unknown keys; duplicate arguments are last-wins | Reject both with errors |
| `RuleBuilder` array and list overloads differ | Fold where an identity exists, in both; reject elsewhere; add the missing overloads; keep `Xnor` |
| Rewrites ignore the rule's options; comparisons take a whole `CompilerOptions` | Default to the rule's options; take the one value used |
| `FaultBudget` code and docs disagree; timeout wording | Abort when faults reach the budget (`>=`); `Timeout` throws and says so |
| Null handling disagrees across parallel predicates | Null is Unknown; collection `IsEmpty` takes `NullBehavior`; `IsNullOrEmpty` is the definite test; `Unknown` is the zero value |
| Rewrite result can exceed the compile cap | Document the caveat; keep both caps |
| Rewrites return three shapes | Keep the split; document it; `AssertSound` overload; one limits table |
| Same mistake, different code per surface | One code per mistake; no renumbering; one code table |
| Argument names are case-sensitive | Case-insensitive; keep `lower`/`upper` vs `min`/`max` and document it |
| `LintRules.All` includes a style lint | `All` = logic lints; `Style` flag holds `NotCanonical` |
| YAML `null` becomes the string "null" | Diagnostic, as JSON |
| Reserved words shadow predicate names | Reject at registration |
| Zero-match array query is silent | Keep ADR-0006; add a trace note |
| Tier 3 polish | One ticket per item. Items needing a naming or API choice are `needs-owner-decision` with a recommended option. |

## Related work

- [predicate-option-defaults](../predicate-option-defaults/spec.md) covers the `ignoreCase` default and the predicate conventions page. Ticket 12 and ticket 07 here add to that page.
- [architecture-deepening-2](../architecture-deepening-2/spec.md) covers threshold and rewrite duplication. Ticket 05 here changes rewrite signatures, so run it before the architecture-deepening-2 tickets that touch the same files, or merge in order.

## Tickets

| Ticket | Change | Blocked by | Status |
| --- | --- | --- | --- |
| [01](issues/01-dsl-threshold-k-is-an-error.md) | DSL threshold k that is not a whole number is a compile error | None | ready-for-agent |
| [02](issues/02-datetime-literal-requires-offset.md) | A date-time literal must carry Z or an offset | None | ready-for-agent |
| [03](issues/03-strict-tree-keys-and-duplicate-arguments.md) | Reject unknown tree keys and duplicate arguments | None | ready-for-agent |
| [04](issues/04-builder-overloads-fold-consistently.md) | RuleBuilder overloads follow one rule | None | ready-for-agent |
| [05](issues/05-rewrites-use-rule-options-and-narrow-parameters.md) | Rewrites and comparisons use the rule options and take a narrow parameter | None | resolved |
| [06](issues/06-fault-budget-reaches-semantics-and-timeout-docs.md) | FaultBudget aborts when faults reach the budget; Timeout is documented | None | resolved |
| [07](issues/07-null-is-unknown-collection-isempty.md) | A null collection is Unknown for IsEmpty and IsNotEmpty | None | resolved |
| [08](issues/08-nullbehavior-unknown-is-zero.md) | NullBehavior.Unknown is the zero value | 07 | ready-for-agent |
| [09](issues/09-document-rewrite-round-trip-caveat.md) | Document that a large rewrite result may not recompile | None | ready-for-agent |
| [10](issues/10-assertsound-compilationresult-overload-and-limits-table.md) | AssertSound accepts the capped rewrites; one limits table | 05 | ready-for-agent |
| [11](issues/11-one-diagnostic-code-per-mistake.md) | One diagnostic code per mistake, and one code table | 01, 03 | ready-for-agent |
| [12](issues/12-argument-names-are-case-insensitive.md) | Argument names are case-insensitive | None | ready-for-agent |
| [13](issues/13-lintrules-style-flag.md) | LintRules.Style holds NotCanonical | None | ready-for-agent |
| [14](issues/14-yaml-null-argument-is-a-diagnostic.md) | A YAML null argument is rejected like JSON | 03 | ready-for-agent |
| [15](issues/15-reject-reserved-predicate-names.md) | Reject predicate names that the DSL treats as keywords | None | ready-for-agent |
| [16](issues/16-zero-match-array-query-trace-note.md) | The trace notes a zero-match array query | None | ready-for-agent |
| [17](issues/17-compilationresult-rule-accessors.md) | CompilationResult has Rule and GetRuleOrThrow | None | ready-for-agent |
| [18](issues/18-print-api-consistency.md) | Print methods follow one shape | None | ready-for-agent |
| [19](issues/19-options-records-have-defaults.md) | Each options record has a Default | None | ready-for-agent |
| [20](issues/20-fake-predicate-label-and-harness-double-call.md) | FakePredicates derive their label; the harness documents its double call | None | ready-for-agent |
| [21](issues/21-precommit-hook-behavior-documented.md) | Document what the pre-commit hook and format-all do | None | ready-for-agent |
| [22](issues/22-assertion-exception-base-type.md) | One exception family for the Testing assertions | None | needs-owner-decision |
| [23](issues/23-project-takes-a-policy-not-a-bool.md) | Decision.Project parameter | None | needs-owner-decision |
| [24](issues/24-compile-entry-point-names.md) | Compile entry point names | None | needs-owner-decision |
| [25](issues/25-predicate-twin-naming-and-placement.md) | Predicate twin naming and string In placement | None | needs-owner-decision |
| [26](issues/26-regex-options-timeout-and-cache.md) | Regex predicate options, timeout and cache | None | needs-owner-decision |
| [27](issues/27-time-window-not-given-sentinel.md) | Time windows use an absent argument, not an empty string | None | needs-owner-decision |
| [28](issues/28-result-record-list-equality.md) | Equality of Decision, CompilationResult and Trace records | None | needs-owner-decision |
| [29](issues/29-measure-versus-limit-names-and-convergence.md) | Measure and limit names; rewrite pass limits | None | needs-owner-decision |
| [30](issues/30-operator-word-casing-in-printed-text.md) | Casing of operator words in canonical text | None | needs-owner-decision |
