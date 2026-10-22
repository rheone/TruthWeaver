# 05: Unknown consistency audit (read-only)

**What to build:** Answer: does `Unknown` follow the principle of least surprise everywhere, with one meaning and no unexpected deviation? Change no behavior. Sweep these areas and build a matrix of "input condition, result, Fault recorded or not, where it is documented": the evaluator and each operator; every predicate family in `TruthWeaver.Predicates` (null input, empty collection, missing key, out-of-range value, parse failure, bad regex, culture); variable references and data sources (no match, several matches); quantifiers over a null or empty collection; the readers and printers (the constant `Unknown`, the letter `U`); the analyzer and `BddManager`; `Decision.IsSatisfied`, `Project` and `Collapse`; and the `TruthWeaver.Testing` assertions.

Judge with these rules: an `Unknown` answer from a predicate is normal and records no Fault; a thrown exception, a predicate's own timeout or a data failure is `Unknown` plus a Fault; `Unknown` never converts to `True` or `False` outside `COALESCE`, `Project` or `Collapse`.

Fold in the open items of `.scratch/main-review-2026-10-17` (05, 06, 07). Link, do not redo, `.scratch/least-surprise-normalization` tickets 07 and 08. Write up each deviation with options and a recommendation. The owner decides each one before any fix ticket is written.

**Blocked by:** None

**Status:** done (owner decisions 2026-10-17; follow-up tickets 10-16)

- [x] The matrix is complete for every area above, with a code reference for each row
- [x] Each deviation lists the cases, the options and a recommendation
- [x] No source file is changed
- [x] The result is in `## Answer`, and the owner has been asked about each deviation, one at a time

## Answer

Read-only audit of the worktree at its base commit. No source file changed. Rules used for the judgement: (R1) a predicate that answers `Unknown` is normal and records no Fault; (R2) a thrown exception, a predicate's own timeout or a data failure is `Unknown` plus a Fault; (R3) `Unknown` never becomes `True` or `False` outside `COALESCE`, `Project` or `Collapse`. Result: R1 and R3 hold everywhere. R2 has one gap in the predicate argument checks (D2), one defensive path (D5) and one trace defect (D3). Three more findings concern the zero value, the tests package and the analyzer (D1, D6, D7), and four are by-design asymmetries to confirm (D4, D8, D9, D10). "Doc" names the page that states the behavior today.

Linked, not redone: `.scratch/least-surprise-normalization` ticket 07 (a null collection is `Unknown` for `IsEmpty`/`IsNotEmpty`, resolved) and ticket 08 (`NullBehavior.Unknown` is the zero value, resolved). Both hold in the code (`CollectionPredicates.BuildEmptiness`, `NullBehavior.cs`).

Fold-in of `.scratch/main-review-2026-10-17`: ticket 05 (lenient unknown predicate records a Fault) holds, because `Evaluator.EvalTermAsync` calls `RecordFault` in the `IsUnknownPredicate` branch. Ticket 06 (trace optional) holds, because `Evaluator.EvaluateAsync` fills `Trace` only when `IncludeTrace` is set. Ticket 07 (variable outcome in the trace) holds at the root of a rule but fails inside a quantifier body: see D3.

### Matrix

Paths are under `src/`; `TW` is `TruthWeaver`.

| # | Area | Input condition | Result | Fault | Code | Doc |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Evaluator, `NOT`/`AND`/`OR`/`XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR`/`PARITY` | An operand is `Unknown` | K3 table: `Unknown` unless the other operand decides it | none | `TW/Ast/K3Logic.cs` (`Not`, `And`, `Or`, `Xor`, `Parity`) | ADR-0001, ADR-0005 |
| 2 | Evaluator, `AND`/`OR`/`COALESCE` | A later operand follows a deciding operand | Skipped, trace entry `NotEvaluated` with a null value (not `Unknown`), unless `Exhaustive` | none | `TW/Evaluation/Evaluator.cs` `EvalChainAsync` | ADR-0002 |
| 3 | Evaluator, `If` | Condition `Unknown` | Both branches run; result is the shared definite value, else `Unknown` | none | `Evaluator.EvalIfAsync` | ADR-0005 |
| 4 | Evaluator, cardinality (`AtLeast`, `AtMost`, `Exactly`, `ExactlyOne`, `ANY`, `ALL`, `NONE`, `BETWEEN`, thresholds) | Some operands `Unknown` | Definite only when the count interval decides it, else `Unknown`; no short-circuit | none | `K3Logic.Threshold`, `Evaluator.EvalAsync` | ADR-0005 decision 6 |
| 5 | Evaluator, `COALESCE`, `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` | An operand is `Unknown` | External operators: first non-`Unknown` operand; inspections always return `True` or `False` | none | `Evaluator.EvalAsync`, `K3Logic.Inspect` | ADR-0005 |
| 6 | `Project`/`Collapse` | Result `Unknown` | The only conversion points: `Project(unknownAs)`, `Collapse(policy)`; both reject a bad argument for every result; `IsSatisfied` stays fail-closed | none | `TW.Abstractions/Decision.cs` | ADR-0001, ADR-0005 decision 12 |
| 7 | Evaluator, predicate returns `Unknown` | Predicate answers `Unknown` directly | `Unknown`, memoized | none (R1) | `Evaluator.InvokeAsync` | ADR-0001, `docs/predicate-conventions.md` |
| 8 | Evaluator, predicate throws | Any exception except an `OperationCanceledException` raised by the evaluation's own token | `Unknown`, memoized, one Fault per term per scope | Fault (R2) | `Evaluator.InvokeAsync` (catch), `RecordFault` | ADR-0001, ADR-0002 |
| 9 | Evaluator, predicate's own timeout or cancellation | `TimeoutException`, `RegexMatchTimeoutException`, or an `OperationCanceledException` while the evaluation token is not cancelled | `Unknown` plus Fault | Fault (R2) | `Evaluator.InvokeAsync` (catch filter) | ADR-0002 |
| 10 | Evaluator, caller token or `EvaluationOptions.Timeout` | Token cancelled or timeout elapsed | `OperationCanceledException` escapes; not a Fault, not `Unknown` | none | `Evaluator.EvalAsync` (`ThrowIfCancellationRequested`), `TW/Evaluation/CompiledRule.cs` | ADR-0002, `EvaluationOptions.cs` |
| 11 | Evaluator, `FaultBudget` | Nth Fault recorded | Remaining nodes skipped (`NotEvaluated`); the fold keeps what was reached, so the faulted term's `Unknown` stays in the result | the Nth Fault | `Evaluator.RecordFault`, `EvalChainAsync` | ADR-0002 |
| 12 | Evaluator, unregistered predicate, `CompilationMode.Lenient` | Term compiled as `IsUnknownPredicate` | `Unknown`, predicate never called | one Fault per term per scope | `Evaluator.EvalTermAsync` | ADR-0002, `TW/Compilation/CompilationMode.cs` |
| 13 | Evaluator, unregistered predicate or collection at run time | Not reachable after a Strict compile | `Unknown` | none (D5) | `Evaluator.InvokeAsync`, `EvalQuantifierAsync` ("Defensive only") | none |
| 14 | Evaluator, class-based predicate with no DI registration | `GetService` returns null | `InvalidOperationException` is absorbed as `Unknown` | Fault (R2) | `Evaluator.InvokeClassBasedAsync` | `docs/predicates.md` |
| 15 | Evaluator, unhandled expression type | Programming error | `InvalidOperationException` escapes `EvaluateAsync` (outside the term try/catch) | none | `Evaluator.EvalAsync` default case | none |
| 16 | String: positive, `NotX`, `IsIn`, `IsEqualTo` | Null selection | `Unknown` by default, `False` under `NullBehavior.False`; `NotX` is the K3 complement | none (R1) | `TW.Predicates/StringPredicates.cs` `Create`, `CreateMembership`, `CreateConfigurable`; `PredicateResult.ForNullAsync` | `docs/predicate-conventions.md` |
| 17 | String: `IsNullOrEmpty`, `IsNullOrWhiteSpace` and twins | Null selection | Always `True`/`False` | none | `StringPredicates.cs` | `docs/predicate-conventions.md` |
| 18 | String: `IsEmpty`, `IsNotEmpty` | Null selection / empty string | Null is `Unknown` (default); empty string is definite | none | `StringPredicates.IsEmpty` | `docs/predicate-conventions.md` |
| 19 | String: culture | Any input | Ordinal comparison, invariant culture; no `culture` argument | n/a | `StringPredicates.cs` | `docs/predicates.md` |
| 20 | Collection (membership, subset, set equality, count) | Null collection | `Unknown` default; `False` under `NullBehavior.False`; `SetEquals` reads null as empty under `False` | none | `TW.Predicates/CollectionPredicates.cs` `Build`, `BuildSetEquals` | `docs/predicate-conventions.md` |
| 21 | Collection: `IsEmpty`/`IsNotEmpty` | Null collection / empty collection | Null follows `NullBehavior`; empty is definite `True`/`False` | none | `CollectionPredicates.BuildEmptiness` | least-surprise ticket 07 |
| 22 | Collection: vacuous cases | Empty argument array (`ContainsAll`) or empty selection (`IsSubsetOf`) | Definite `True` | none | `CollectionPredicates.ContainsAll`, `IsSubsetOf` | predicate descriptions |
| 23 | Collection: definite null test | Null collection | No predicate exists (D10) | n/a | `CollectionPredicates.cs` | none |
| 24 | Numeric, scalar, date-time compare, membership, default | Null selection | `Unknown` default, `False` under `NullBehavior.False`; the argument is read before the selection | none | `TW.Predicates/ScalarPredicateCore.cs` `Compare`, `Membership`, `DefaultTest` | `docs/predicate-conventions.md` |
| 25 | Numeric, scalar, date-time null tests | Null selection | Always `True`/`False` | none | `ScalarPredicateCore.NullTest` | `docs/predicate-conventions.md` |
| 26 | Numeric, scalar, date-time range | Literal reversed bounds | Compile diagnostic `TRE0026` | n/a | `ScalarPredicateCore.ReversedBounds` | `docs/predicates.md` |
| 27 | Same | Non-literal reversed bounds | `ArgumentException` before the selection is read, so `Unknown` plus Fault even for a null selection | Fault (R2) | `ScalarPredicateCore.Range` | `docs/predicates.md` |
| 28 | Numeric out of range | `long` or `decimal` argument at the type limits | Definite compare; no overflow path | none | `NumericPredicates.cs` | none |
| 29 | Date-time clock (`IsAfterNow`, `IsBeforeNow`) | Null selection | Follows `NullBehavior`; the clock is read per evaluation from `TimeProvider` | none | `TW.Predicates/DateTimePredicates.cs` `Clock` | `docs/predicates.md` |
| 30 | Date-time fixed-offset and calendar tests | Bad argument from a data source | `ArgumentException` before the selection is read: `Unknown` plus Fault even for a null selection | Fault (R2) | `TW.Predicates/DateTimePredicates.FixedOffset.cs` `FixedOffsetPredicate` | `docs/predicates.md` |
| 31 | Date-time argument from a data source | String without an offset | `TypeMismatch` Fault with the fix text | Fault (data) | `TW/Evaluation/VariableConversion.cs` `OffsetFixFor` | `docs/data-sources.md` |
| 32 | Regex match | Null selection | `Unknown` default; the pattern and flags are not read | none | `TW.Predicates/RegexPredicates.cs` `Create` (line 175) | `docs/predicate-conventions.md` |
| 33 | Regex match | Invalid pattern or flags from a data source, non-null selection | `ArgumentException` or the regex parse exception is absorbed: `Unknown` plus Fault | Fault (R2) | `RegexPredicates.ParseFlags`, `RegexPatternCache.Get` | `docs/predicates.md` |
| 34 | Regex match | Invalid pattern or flags from a data source, null selection | `Unknown`, no Fault (D2) | none | `RegexPredicates.Create` | `docs/predicates.md` (silent) |
| 35 | Regex timeout | Match exceeds one second | `RegexMatchTimeoutException`: `Unknown` plus Fault | Fault (R2) | `RegexPredicates.MatchTimeout` | `docs/predicate-conventions.md` |
| 36 | Regex invalid literal | Pattern or flags literal | Compile error `TRE0026` | n/a | `RegexPredicates.ValidateArguments` | `docs/predicates.md` |
| 37 | Type tests | Null selection | `Unknown` default | none | `TW.Predicates/TypePredicates.cs` `Create` | `docs/predicates.md` |
| 38 | Type tests | Parse failure (`"abc"` for `IsNumeric`, `IsGuid`, `IsDateTimeOffset`, `IsUrl`) | Definite `False`; `NaN` and infinity are not numeric; invariant culture | none | `TypePredicates.NumericTest`, `GuidTest`, `DateTimeOffsetTest` | `docs/predicates.md` |
| 39 | `SelectedValuePredicates` | `test` or `select` returns `Unknown` / throws | `Unknown` without Fault / with Fault | by R1 / R2 | `TW.Predicates/SelectedValuePredicates.cs` | `docs/predicates.md` |
| 40 | Variable reference, scalar | Query matches nothing | `Unknown` plus Fault (`Missing`); the predicate is not called | Fault (R2) | `VariableConversion.TryConvert`, `Evaluator.ResolveVariablesAsync` | `docs/data-sources.md`, ADR-0006 |
| 41 | Variable reference, scalar | Query matches several nodes | `Unknown` plus Fault (`Ambiguous`) | Fault (R2) | same | `docs/data-sources.md` |
| 42 | Variable reference | Match has the wrong kind, a JSON `null`, an object or an array node | `Unknown` plus Fault (`TypeMismatch`; `null` reports `UnsupportedType`) | Fault (R2) | `TW.DataSources.Json/JsonDataSource.cs` `TryConvert`, `Evaluator.QuerySourceUncachedAsync` | `docs/data-sources.md` (D4) |
| 43 | Variable reference, array argument | Query matches nothing | Empty array, valid, no Fault; the trace adds `[no match for ...]` | none | `VariableConversion.TryConvert`, `Evaluator.ResolveVariablesAsync` | `docs/data-sources.md` (D4) |
| 44 | Variable reference | Source not supplied, source throws, returns null, malformed query, own timeout | `Unknown` plus Fault; evaluation cancellation propagates | Fault (R2) | `Evaluator.QuerySourceUncachedAsync` | `docs/data-sources.md` |
| 45 | Variable reference, trace | Resolved, missing, ambiguous and so on | `[ref: outcome]` after the term, but only outside a quantifier body (D3) | n/a | `Evaluator.Annotated`, `Evaluator.cs` line 698 | ADR-0006 decision 13 |
| 46 | Quantifier, collection | Null collection | `Unknown`, no Fault, trace `[no collection]` | none | `TW/Registry/CollectionBinding.cs` (`elements is null`) | ADR-0010 decision 4 |
| 47 | Quantifier, collection | Empty collection | `FORALL` is `True`, `EXISTS` is `False` | none | `CollectionBinding.cs` (`elements.Count == 0`) | ADR-0010 decision 4 |
| 48 | Quantifier, collection | Selector throws | `Unknown` plus one Fault for the collection | Fault (R2) | `CollectionBinding.cs` (materialization catch) | ADR-0010 decision 4 |
| 49 | Quantifier, element body | Elements `Unknown`/`False`/`True` | `FORALL` is K3 `AND`, `EXISTS` is K3 `OR`, stops on the deciding value | none | `CollectionBinding.cs` | ADR-0010 |
| 50 | Quantifier, `MaxElementEvaluations` | Bound reached | That quantifier is forced `Unknown` plus one Fault; evaluation aborts | Fault (R2) | `CollectionBinding.cs` (`elementLimitExceeded`) | ADR-0010 decision 6 |
| 51 | Quantifier, unregistered collection | Compile time | Error `UnknownCollection`, also under `Lenient` | n/a | `TW/Compilation/RuleNodeCompiler.cs` `BuildQuantifier` | `CompilationMode.cs` (predicates only) |
| 52 | Readers, DSL | `Unknown`, `UNKNOWN`, `U` (any case) | `Unknown` constant; `U(` is a call to a reserved name | n/a | `TW/Parsing/DslParser.cs` (lines 574 to 597), `ReservedWords.cs` | `docs/rule-text.md` |
| 53 | Readers, JSON and YAML | `true`, `false`, `"unknown"` (any case) | Constants; `null`, a number or a letter `"U"` is a diagnostic | n/a | `TW/Json/JsonNodeCursor.cs` `TryGetTruthValue`, `TW.Yaml/YamlNodeCursor.cs` | `docs/rule-formats.md` (D9) |
| 54 | Readers, rule-test file and decision JSON | Words or letters / a null result | Strict; an evaluated node with a null result is rejected, a skipped node needs one | n/a | `TW.Testing/RuleTestFileReader.cs`, `TW/Json/DecisionJsonReader.cs` | `docs/decision-records.md`, `docs/rule-tests.md` |
| 55 | `TruthValueText.TryParse*` | Text not recognised | Returns `false` with `out value` set to `False` (the enum zero value, D1) | n/a | `TW/Ast/TruthValueText.cs` | none |
| 56 | Printers, constants | `Unknown` | `Unknown` (Word), `U` (Letters, LogicSymbols), `unknown` (tree formats); `U` has no symbol | n/a | `TW/Printing/ConstantFormat.cs`, `TreeFormatEmitter.cs` | `docs/rule-text.md`, `docs/truth-tables.md` |
| 57 | Printers, render tree and truth tables | Skipped node vs evaluated `Unknown` | Skipped is `NoData`; evaluated `Unknown` is `Indeterminate` | n/a | `TW/Printing/RuleRenderTree.cs` `StateFor`, `TruthTablePrinter.cs` | `docs/truth-tables.md` |
| 58 | Analyzer and `BddManager` | Any term | Dual-rail BDD: each term has independent True and Unknown variables; a tautology is reported only when `True` for every `{T, F, U}` assignment; `BddManager` itself is two-valued | none | `TW/Analysis/Analyzer.cs` `Build`, `TW/Analysis/BddManager.cs` | ADR-0005 decision 17 |
| 59 | Analyzer, term correlation | Two terms that read the same missing value | Treated as independent; an impossible mixed assignment can appear in a witness | none | `Analyzer.Build` (`TermExpression`) | none (D6) |
| 60 | Analyzer, term cap | More terms than `MaxAnalysisTerms` | `Undecided` (a named outcome), never `Unknown` | none | `RuleEquivalenceOutcome`, `RuleImplicationOutcome`, `RuleOverlapOutcome`, `CounterfactualOutcome`, `RuleRelation` | `docs/counterfactuals.md` |
| 61 | Counterfactual | A term that faulted | Taken as `Unknown`, as the decision recorded it | none | `TW/Analysis/RuleCounterfactual.cs` | `docs/counterfactuals.md` |
| 62 | `Decision` | Result `Unknown` | `IsSatisfied` is `false`; `Faults` may be empty (a predicate answered `Unknown`) | none | `TW.Abstractions/Decision.cs` | ADR-0001 |
| 63 | Testing: `StubValues`, `StubRuleCompiler` | Term with no stub value | `Unknown`, no Fault; the rule-test file reader rejects a `given` term the rule does not use | none | `TW.Testing/StubValues.cs`, `RuleTestFile.cs` (line 264) | `docs/rule-tests.md` (D8) |
| 64 | Testing: `FakePredicates.Scripted` | More calls than script entries | `InvalidOperationException`; inside a rule it becomes `Unknown` plus Fault | Fault (R2) | `TW.Testing/FakePredicates.cs` (line 130) | `docs/testing-assertions.md` (D7) |
| 65 | Testing: `DecisionAssertions` | `NotBeSatisfied` | Passes for `False` and `Unknown`; `HaveResult(Unknown)` is the exact check | n/a | `TW.Testing/DecisionAssertions.cs` | `docs/testing-assertions.md` |
| 66 | Testing: `PredicateHarness` | Predicate returns `Unknown` / throws | Reported as "Returned Unknown without a fault" / "Threw ... a Fault is recorded"; same exception filter as the evaluator | by R1 / R2 | `TW.Testing/PredicateHarness.cs` | `docs/predicate-harness.md` |
| 67 | Enum zero value | `default(TruthValue)` | `False` (D1) | n/a | `TW.Abstractions/TruthValue.cs` | none |

### Deviations

Each needs an owner decision before a fix ticket is written. They are ordered by how likely they are to surprise a user.

**D1. `default(TruthValue)` is `False`.** `TruthValue` declares `False`, `True`, `Unknown` in that order, so an uninitialized field, a `default` array element or an `out` value on a failed parse reads as `False`, the one answer this library avoids guessing. Cases: `TruthValueText.TryParse` and `TryParseLetter` set `False` on failure (row 55); any host `TruthValue` field that is never set (row 67). `NullBehavior` already moved `Unknown` to 0 (least-surprise ticket 08), so the library is inconsistent with itself.
- (a) Reorder the enum to `Unknown = 0, False = 1, True = 2`. Breaking for code or stored data that uses the numeric values; the first release has not shipped, so the cost is low.
- (b) Keep the order and document the hazard in `TruthValue` and in the glossary.
- (c) Keep the order and set `out` values on failed parses to `Unknown` explicitly.
- Recommendation: (a). It is the decision the owner took for `NullBehavior`, and it fixes every `default` at once. It needs a check that no code or serialized form depends on the numeric order (the JSON forms use names).

**D2. A bad regex pattern or flags value from a data source is a Fault only when the selection is non-null.** `RegexPredicates.Create` returns for a null selection (line 175) before it reads the pattern and flags, so the data-source value is never checked and the term is `Unknown` with no Fault. `ScalarPredicateCore.Range` and the calendar predicates check their arguments first, so they fault even for a null selection. Cases: `IsMatch` and `IsNotMatch` with `pattern` or `flags` from `from(...)` and a null selected string (row 34 against rows 27 and 30). The authoring error stays hidden until the first non-null value.
- (a) Parse the flags and build the `Regex` before the selection is read, as `Range` does. The cost is one cache lookup per null selection.
- (b) Keep it and state in `docs/predicates.md` that the pattern is checked only for a non-null selection.
- Recommendation: (a). It matches the other argument checks, and R2 says a data failure is `Unknown` plus a Fault.

**D3. The variable-resolution outcome note is lost inside a quantifier body.** `ResolveVariablesAsync` stores the note under `(this.scope, identity)` (`Evaluator.cs` line 698) but `Annotated` reads `(this.memoScope, identity)`. Outside a quantifier both are `null`, so it works. Inside one, `scope` is built from the bound variable (`line[2]`) and `memoScope` from the collection name (`lines[2]`), so the keys differ and the trace shows no `[ref: resolved]` or `[ref: missing]`. `ValueDescriptions` and `ZeroMatchReferences` use `memoScope` and are not affected. `EvaluationState.VariableOutcomes` says it is "keyed the same way as `Memo`", so the code disagrees with its own doc. This is main-review ticket 07 not yet true for element bodies.
- (a) Key the write by `memoScope`. A one-line fix plus a test that resolves a `from(...)` reference inside a `FORALL` with `IncludeTrace` on.
- (b) Amend ADR-0006 decision 13 to say the note appears at the root only.
- Recommendation: (a).

**D4. "No value" has different answers by source, on purpose.** A selector that returns `null` is `Unknown` with no Fault (`NullBehavior`). A scalar data-source query that matches nothing, matches several nodes or hits a JSON `null` is `Unknown` plus a Fault. An array-argument query that matches nothing is a valid empty array with no Fault (a JSON `null` element in it is a Fault). A null quantifier collection is `Unknown` with no Fault. Each case is documented and follows R1 or R2 (rows 20, 40 to 43, 46), so this is a confirmation, not a defect.
- (a) Keep. Add one table to `docs/data-sources.md` that lists the four cases side by side, with the reason: a selector is host code that may have no value, a query is a rule author's request for a value that is missing.
- (b) Make an empty array query a `Missing` Fault, so it matches the scalar case. Breaks rules that rely on an empty array.
- (c) Make a missing scalar `Unknown` with no Fault. Hides missing data.
- Recommendation: (a).

**D5. Two defensive evaluator paths return `Unknown` with no Fault.** `Evaluator.InvokeAsync` (unregistered predicate, line 575) and `EvalQuantifierAsync` (unregistered collection, line 429) return `Unknown` silently. A compiled rule keeps the registry it was compiled with, so they should not run; if they ever do, `Decision.Faults` gives no reason for the `Unknown`.
- (a) Record a Fault (`KeyNotFoundException`) in both, as the Lenient path does.
- (b) Throw `InvalidOperationException`, as the unhandled-expression case does (row 15).
- (c) Keep.
- Recommendation: (a). It costs two lines and keeps R2 true without exceptions used as control flow.

**D6. The analyzer treats terms as independent.** Two terms that read the same missing value become `Unknown` together at run time, but the dual-rail model allows `A = True, B = Unknown`. The results stay sound for tautology and contradiction diagnostics (fewer are reported), but a counter-example, an overlap pair or a `Linter` finding can name an assignment that cannot occur. A term that never answers `Unknown` (`IsNull`, or any predicate under `NullBehavior.False`) is also treated as possibly `Unknown`, so `IsUnknown(IsNull(x))` is not linted.
- (a) Keep and state the assumption in `docs/counterfactuals.md` and on the linter page.
- (b) Add an optional "never Unknown" flag to `PredicateSchema` that the analyzer reads. A larger API change.
- Recommendation: (a) now; (b) only if users report noisy results.

**D7. `FakePredicates.Scripted` says it "fails loudly", but the evaluator absorbs the exception.** An under-scripted fake throws `InvalidOperationException`; inside a rule that becomes `Unknown` plus a Fault, so a test that checks only `Result` sees `Unknown` and can pass (for example `HaveResult(Unknown)`).
- (a) Change the XML text and `docs/testing-assertions.md` to say "the term is `Unknown` with a Fault; assert `HaveNoFaults()`". No behavior change.
- (b) Keep the text.
- Recommendation: (a).

**D8. A term with no stub value is `Unknown`, so a misspelt key hides itself in programmatic `StubValues`.** The rule-test file reader already rejects a `given` term the rule does not use (`RuleTestFile.cs` line 264); `StubValues.Create` and `StubRuleCompiler` do not. This is documented, and an `Unknown` default is the K3 reading.
- (a) Keep.
- (b) Add an opt-in check that reports stub keys the compiled rule never reads.
- Recommendation: (a). The file path already covers the common case.

**D9. The constant letters `T`, `F`, `U` are accepted in the DSL, the rule-test file and printer overrides, but not in JSON or YAML rules.** `{"const": "U"}` is a diagnostic; `"unknown"` works (rows 52 to 56). `docs/rule-formats.md` says the letters are not in the tree, so this is documented.
- (a) Keep. The diagnostic is clear and the tree formats stay strict.
- (b) Accept the letters in `TruthValueText.TryParse` for all readers.
- Recommendation: (a).

**D10. Collection predicates have no definite null test.** Strings have `IsNullOrEmpty` and `IsNullOrWhiteSpace`; scalars have `IsNull` and `IsNotNull`; `CollectionPredicates` has none (row 23). With `NullBehavior.Unknown` a null collection is `Unknown` for every collection predicate, and `NullBehavior.False` makes `IsEmpty` `False` for null, so a host cannot ask "null or empty" in a rule. Least-surprise ticket 07 names `IsNullOrEmpty` as the definite test, which holds for strings only.
- (a) Add `CollectionPredicates.IsNullOrEmpty` and `IsNotNullOrEmpty` (a definite pair, no `NullBehavior`), with their `docs/strong-k3/predicates` pages and the K3 reference sync.
- (b) Keep; the host registers `IsEmpty` with the `NullBehavior` it wants.
- Recommendation: (a), but it adds public API, so confirm before it is ticketed.

### Checked and consistent

- `NotX` twins complement for a null selection too, and agree with `NOT predicate` (rows 16, 20, 24).
- Culture is invariant for every parse and compare (rows 19, 38).
- Skipped nodes are `null` in the trace, not `Unknown` (rows 2, 57), and the render tree keeps them apart.
- Analysis limits return `Undecided`, not `Unknown` (row 60).
- `Project` and `Collapse` are the only `Unknown` to definite conversions outside `COALESCE`, and `IsSatisfied` ignores them (rows 6, 62).

### Owner decisions

Pending. The last acceptance box needs the owner to be asked about D1 to D10 one at a time (project rule 8). That interview has not run.

### Owner decisions (2026-10-17)

D1: reorder `TruthValue` (ticket 10). D2: validate pattern and flags first (11). D3 and D5: fix and record faults (12). D4: keep, document (16). D6: keep, document (16). D7: loud only on misuse, the exact shape is the implementer's to propose (13). D8: keep. D9: accept `T`/`F`/`U` in JSON and YAML (14; the audit recommended keeping them rejected). D10: add `IsNullOrEmpty` and its twin (15).
