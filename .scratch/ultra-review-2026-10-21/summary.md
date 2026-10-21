# Ultra review summary - 2026-10-21

Scope: bounded K3-first review of `src/` (see `plan.md`). 14 reviewers, 96 raw findings, merged into 67 tickets.

Tickets by severity: critical 0, high 8, medium 31, low 28.

Verification: each critical or high candidate was checked by a fresh agent. 25 findings were confirmed (some at a lower severity) and none were refuted. Medium and low findings that were not verified are marked PLAUSIBLE. No reviewer rated anything critical.

Not yet done: the K3 oracle test (plan phase 1), so no truth-table mismatch has been checked mechanically. Reviewers read truth tables from the docs and code.

| # | Severity | Verdict | Title | Findings |
| --- | --- | --- | --- | --- |
| [01](issues/01-fault-budget-abort-drops-skipped-operands.md) | high | CONFIRMED | A fault-budget abort drops skipped AND/OR/COALESCE/quantifier operands, so an aborted evaluation can return a definite result | F4.1, F9.3 |
| [02](issues/02-dsl-parameter-named-tfu-read-as-constant.md) | high | CONFIRMED | A DSL definition parameter named T, F, U, True, False or Unknown is read as a constant; JSON, YAML and the builder read it as the parameter | F2.1, L3.1 |
| [03](issues/03-parser-stack-overflow-no-depth-guard.md) | high | CONFIRMED | Deep nesting (DSL) and self-referencing aliases or deep nesting (YAML) overflow the stack and crash the process | F2.2, T2.1 |
| [04](issues/04-definition-expansion-exponential.md) | high | CONFIRMED | Definition (let) expansion grows exponentially before any size limit is checked | F2.4, F3.1 |
| [05](issues/05-yaml-alias-exponential-expansion.md) | high | CONFIRMED | YAML anchors and aliases allow exponential rule expansion before any size limit runs | T2.2 |
| [06](issues/06-atleast-bdd-exponential.md) | high | CONFIRMED | AtLeastBdd recursion is exponential in operand count, so Compile can hang | F5.1 |
| [07](issues/07-nand-nor-expander-throws-on-quantifiers.md) | high | CONFIRMED | ExpandToNand and ExpandToNor throw on any rule that contains FORALL or EXISTS | F5.2 |
| [08](issues/08-render-decision-with-quantifier-throws.md) | high | CONFIRMED | Rendering a decision with an evaluated quantifier throws: the trace shape does not match the outline | T2.5 |
| [09](issues/09-fault-budget-exhaustive-changes-result.md) | medium | CONFIRMED | With FaultBudget set, Exhaustive mode changes Decision.Result, against the ADR-0002 invariant | F4.2 |
| [10](issues/10-maxnodecount-resets-in-quantifier.md) | medium | CONFIRMED | MaxNodeCount restarts at 0 inside every quantifier body, so the total node limit is not enforced | F3.2 |
| [11](issues/11-quantifier-atoms-keyed-by-text-equivalence.md) | medium | CONFIRMED | Quantifier atoms keyed by text make RuleEquivalence call valid ToNnf rewrites NotEquivalent | F5.3 |
| [12](issues/12-quantifier-trace-label-case-mismatch.md) | medium | CONFIRMED | Quantifier trace label uses the registered collection name; canonical text uses the name as written | L2.1 |
| [13](issues/13-json-duplicate-keys-accepted.md) | medium | CONFIRMED | JSON rules with duplicate keys are accepted (last wins); YAML rejects the same document | F2.3, T2.3 |
| [14](issues/14-json-depth-cap-64.md) | medium | CONFIRMED | JSON depth is capped at the JsonDocument default of 64, below the DSL MaxDepth; RuleBuilder.Compile throws JsonException | T2.4, F2.7 |
| [15](issues/15-lenient-literal-guessing-data-loss.md) | medium | CONFIRMED | Lenient literal guessing silently changes or drops argument values (data loss on tool conversion) | F3.3 |
| [16](issues/16-regex-culture-invariant.md) | medium | CONFIRMED | Regex is built without CultureInvariant: an inline (?i) depends on the culture of the first thread to compile it | F7.1 |
| [17](issues/17-isguid-accepts-whitespace.md) | medium | CONFIRMED | IsGuid accepts surrounding whitespace, but the doc says it does not trim | F7.2 |
| [18](issues/18-policy-collection-faults-dropped.md) | medium | CONFIRMED | Collection-level faults (selector, element limit, missing collection) are dropped from PolicyDecision.Decision.Faults | F9.1, F4.3 |
| [19](issues/19-rulebuilder-fold-vs-reject-docs-drift.md) | medium | CONFIRMED | K3 reference pages disagree with RuleBuilder on folding versus rejecting too few operands (And, Or, ANY/ALL/NONE, Parity, Coalesce, GreaterThan/LessThan overloads) | L1.1, L1.2, L2.2, L2.3, L3.4 |
| [20](issues/20-duplicate-definition-parameters.md) | medium | PLAUSIBLE | Duplicate parameter names in one definition are accepted; the last binding silently wins | F2.6, F3.5, L3.2 |
| [21](issues/21-note-on-use-dropped.md) | medium | PLAUSIBLE | A note written on a use node is dropped when the use expands | L3.3 |
| [22](issues/22-unreachable-definitions-not-validated.md) | medium | PLAUSIBLE | Definitions not reachable from the rule body are never validated | F3.4 |
| [23](issues/23-sourcesof-hardcodes-definition-depth.md) | medium | PLAUSIBLE | RuleDefinitions.SourcesOf hard-codes the default MaxDefinitionDepth | F3.6 |
| [24](issues/24-analysis-no-node-budget.md) | medium | PLAUSIBLE | Compile-time analysis and the other BDD readers run with no node budget | F5.5 |
| [25](issues/25-comment-note-attachment-quadratic.md) | medium | PLAUSIBLE | Comment-to-note attachment is quadratic and runs before any size limit | F2.5 |
| [26](issues/26-if-monotone-contradiction.md) | medium | PLAUSIBLE | NormalForms and rewriting-rules.md treat If as not information-monotone; the K3 reference says it is monotone | F5.4, L3.8 |
| [27](issues/27-literalvalue-equality-vs-printing.md) | medium | PLAUSIBLE | LiteralValue equality ignores DateTimeOffset offset and decimal scale, but the printers keep both | F1.1 |
| [28](issues/28-regex-literal-checked-without-flags.md) | medium | PLAUSIBLE | A literal regex pattern is checked with no options when the flags come from a data source | F7.3 |
| [29](issues/29-calendar-predicates-fault-near-minmax.md) | medium | PLAUSIBLE | Calendar predicates fault on a valid instant near DateTimeOffset.MinValue/MaxValue, including default(DateTimeOffset) | F7.4 |
| [30](issues/30-json-datasource-clr-values-throw.md) | medium | PLAUSIBLE | JsonDataSource throws instead of converting when a node holds a CLR value (Guid, DateTimeOffset, char, NaN) | F8.2 |
| [31](issues/31-datasource-result-classification.md) | medium | PLAUSIBLE | NoMatch, AmbiguousMatch and non-scalar matches are classified as SourceError or TypeMismatch inconsistently between TryGetAsync/GetAsync, variables and the evaluator | F8.1, L5.6 |
| [32](issues/32-predicate-names-unparsable-in-dsl.md) | medium | PLAUSIBLE | Registry and generator accept predicate and collection names that rule text cannot express | F8.3 |
| [33](issues/33-quantifier-variable-shadows-source-validator.md) | medium | PLAUSIBLE | A quantifier variable that shadows a declared source keeps the hidden source validator for element queries | F8.4 |
| [34](issues/34-policy-shared-memo-across-registries.md) | medium | PLAUSIBLE | The term memo is shared across rules compiled with different registries in one PolicySet | F9.4 |
| [35](issues/35-policy-maxelementevaluations-scope.md) | medium | PLAUSIBLE | MaxElementEvaluations is a whole-set bound but documented as per evaluation | F9.5 |
| [36](issues/36-invalid-utf16-json-throws.md) | medium | PLAUSIBLE | Invalid UTF-16 in JSON input throws ArgumentException instead of returning a diagnostic | T2.6 |
| [37](issues/37-builder-accepts-nan-infinity.md) | medium | PLAUSIBLE | RuleBuilder accepts double NaN and Infinity arguments, then Compile throws | T2.7 |
| [38](issues/38-adr-0005-constant-letters-docs.md) | medium | PLAUSIBLE | rule-text.md, ADR-0005 and the const diagnostic disagree with the parsers about which constant spellings JSON and YAML read | L3.5, L5.2, L5.3 |
| [39](issues/39-adr-0005-stale-references.md) | medium | PLAUSIBLE | ADR-0005 cites a Project(bool) signature, diagnostic codes and a decision number that the code and docs do not use | F1.4, L3.7, L5.1, L1.3, L2.4, L3.6, F1.6 |
| [40](issues/40-policy-fault-budget-priority.md) | low | CONFIRMED | A shared fault budget makes the overriding policy outcome depend on priority, against ADR-0015 section 4 | F9.2 |
| [41](issues/41-policy-trace-not-returned.md) | low | PLAUSIBLE | IncludeTrace collects a trace that PolicyDecision never returns | F9.6 |
| [42](issues/42-factory-argument-validation.md) | low | PLAUSIBLE | Predicate factories and lambda registration accept null delegates, null selectors and undefined NullBehavior values, then fail on every evaluation | F8.7, F6.4, F7.6 |
| [43](issues/43-tryget-source-exception.md) | low | PLAUSIBLE | TryGetAsync does not catch a source exception, so its result-instead-of-throwing contract fails | F8.5 |
| [44](issues/44-generator-case-colliding-arg-names.md) | low | PLAUSIBLE | The generator does not detect argument names that differ only in case; Register throws at run time | F8.6 |
| [45](issues/45-element-requery-vs-adr.md) | low | PLAUSIBLE | The element projector and outer data sources run per distinct query, not as ADR-0010 and ADR-0006 decision 11 state | F8.8, F8.9 |
| [46](issues/46-yaml-datasource-nonscalar-keys.md) | low | PLAUSIBLE | YAML mapping keys that are not scalars collapse to an empty-string key and overwrite data | F8.10 |
| [47](issues/47-datasource-faultkind-docs.md) | low | PLAUSIBLE | docs/data-sources.md names a fault kind UnsupportedType that faults never carry | F8.11, L5.5 |
| [48](issues/48-yaml-const-empty-reports-found.md) | low | PLAUSIBLE | YAML const: with an empty or ~ value reports Found as a quoted empty string rather than null | L5.7 |
| [49](issues/49-yaml-multi-document-silent.md) | low | PLAUSIBLE | A multi-document YAML stream silently compiles only the first document | T2.9 |
| [50](issues/50-yaml-duplicate-key-rescan-label.md) | low | PLAUSIBLE | YAML duplicate-key rescan labels any repeat under a key named 'args' as DuplicateArgument | T2.8 |
| [51](issues/51-collection-fault-not-recorded.md) | low | PLAUSIBLE | Collection-level faults skip RecordFault: no log, no fault metric | F4.5 |
| [52](issues/52-flat-trace-missing-operator-nodes.md) | low | PLAUSIBLE | The flat trace logs no operator nodes, but CONTEXT.md and EvaluationOptions.IncludeTrace say it does | F4.4 |
| [53](issues/53-timeout-options-validation.md) | low | PLAUSIBLE | Timeout and MaxElementEvaluations are not validated; Timeout is cooperative only | F4.6 |
| [54](issues/54-evaluator-hardcoded-trace-labels.md) | low | PLAUSIBLE | Evaluator hard-codes operator trace labels instead of reading OperatorDefinitions | F4.7 |
| [55](issues/55-parser-error-recovery-cascade.md) | low | PLAUSIBLE | DSL error recovery cascades into misleading follow-on diagnostics, and reserved words in operand position give a reserved-name error | F2.8, F2.9 |
| [56](issues/56-treepath-escape-backslash.md) | low | PLAUSIBLE | The TreePath bracket form does not escape backslash, so two different keys can share one path | F2.10 |
| [57](issues/57-rule-text-grammar-from-omitted.md) | low | PLAUSIBLE | The rule-text grammar omits the from(...) variable-reference argument value | F2.11 |
| [58](issues/58-between-bound-error-span.md) | low | PLAUSIBLE | BETWEEN bound errors point at the whole node instead of its min or max member | F3.7 |
| [59](issues/59-operatorinfo-quantifier-describe.md) | low | PLAUSIBLE | OperatorInfo.Describe returns Forall/Exists with an empty description; tree-format suggestions omit forall/exists | F1.2, F1.3 |
| [60](issues/60-equivalent-description-formerly-xnor.md) | low | PLAUSIBLE | The EQUIVALENT description calls XNOR formerly, but XNOR is a current alias | F1.5 |
| [61](issues/61-numeric-isbetween-labels.md) | low | PLAUSIBLE | Numeric IsBetween and IsNotBetween default labels do not match the reference pages | F6.1 |
| [62](issues/62-predicates-index-null-element-docs.md) | low | PLAUSIBLE | The predicates index says a null element has no effect outside HasCount, but IsSubsetOf and SetEquals answer False | F6.2 |
| [63](issues/63-hascount-negative-count.md) | low | PLAUSIBLE | Collection HasCount predicates accept a negative count with no diagnostic | F6.3 |
| [64](issues/64-fixedoffset-trailing-newline.md) | low | PLAUSIBLE | FixedOffsetText regexes use '$', so a trailing newline is accepted | F7.5 |
| [65](issues/65-nullbehaviorscope-dispose-order.md) | low | PLAUSIBLE | Disposing NullBehaviorScope instances out of order can restore a disposed scope | F7.7 |
| [66](issues/66-simplify-docs-between-none.md) | low | PLAUSIBLE | BETWEEN and NONE pages say Simplify keeps the node; Simplify rewrites it when an operand is a constant | L2.5 |
| [67](issues/67-explainer-faulted-vs-skipped-wording.md) | low | PLAUSIBLE | The explainer words for faulted (could not be evaluated) and skipped (not evaluated) are near-identical | L5.4 |
