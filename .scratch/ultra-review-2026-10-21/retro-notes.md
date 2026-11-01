# Retro notes - ultra-review remaining run (2026-11-01)

Untracked working file. Agents append one block per ticket; the coordinator adds one block per branch and reads it all at the end for the retro (friction, wrong tickets, process gaps, docs or tooling to change).

Block format per ticket:

```
## Ticket NN (branch) - <outcome: done|stale|wontfix|failed>
- Ticket accurate? (location, claim, repro still true; what was off)
- Surprises in the code or docs
- TDD: red test first? what was awkward to test
- Review findings that changed the work
- Commit hook / test run time, any flaky or slow step
- Docs pages touched, any doc-lint or doctest friction
- Process friction (skills, CLAUDE.md, scripts, missing tooling) and a suggested fix
```

## Ticket 13 (fix/parser-compilation-surfaces) - done
- Ticket accurate? Yes; repro true. The ticket's "trivial fix" (`AllowDuplicateProperties = false`) is wrong for rule trees: it would turn a repeated `args` name into a JSON syntax error and lose TRE0032, which an existing test pins. Fix went in `TreeFormatReader.CheckKeys` instead.
- Surprises: duplicates also slip through outside CheckKeys (variable-reference `from`/`query`, root `definitions` hidden by KeyExcludingCursor). Covered them too; duplicate definition *names* left alone (not a structural key).
- TDD: red first, 9 theory rows, all red. Sonar S4144 flagged the new theory as identical to an existing one: had to add a message assertion to make it distinct.
- Review findings: inline two-axis review only (no spawning); nothing changed the work.
- Hook: 305 s total (doc-tests pass adds ~42 s when .md is staged). format-all.cs ~4 min (Roslynator 58 s).
- Docs: syntax.md, strong-k3 diagnostics.md, docs/diagnostics.md; no lint friction.
- Process friction: the harness moves any foreground command over ~180 s to background, so format-all and git commit both need background + wait. Suggest the brief say "always run format-all and commit with run_in_background".

## Ticket 36 (fix/parser-compilation-surfaces) - done
- Ticket accurate? JSON and decision-record parts yes. The RuleBuilder part was wrong in kind: the builder never threw; the JSON writer silently swapped the lone surrogate for U+FFFD, so the rule compiled with a different value (worse than a throw). Fixed as MalformedTree at `$.args.<name>`.
- Surprises: xUnit theory-data serialization replaces a lone surrogate with U+FFFD, so an `[InlineData("a\uD800")]` red test was "red" for the wrong reason (TRE0002 from a mangled name). Had to build payloads in the test body. Worth a line in the testing docs.
- TDD: red first; the first red was misleading as above. Sonar S127 (loop variable change) forced a Rune.DecodeFromUtf16 loop.
- Review findings: own Spec-axis pass found four more `JsonDocument.Parse(string)` call sites in Tool.Core with the same exception; listed in Resolution, not fixed (out of scope).
- Hook / test time: format-all ~4.5 min (Roslynator 72 s).
- Docs: strong-k3 diagnostics.md, decision-records.md, rulebuilder.md. No lint friction.
- Process friction: while the hook runs (5 min) I cannot edit src/tests, because the hook builds the working tree; serial tickets on one checkout idle ~10 min per ticket on format+hook. Suggest a worktree per commit or letting the hook skip the separate format-all run (the hook already formats).

## Ticket 37 (fix/parser-compilation-surfaces) - done
- Ticket accurate? Yes, reproduced exactly as written; the fix site (PredicateBuilder.CollectProblems) was right.
- Surprises: none; it sits next to the ticket 36 surrogate check in the same method, so 36 and 37 touch the same lines (handy on one branch, a conflict if split).
- TDD: red first (3 throws). Finite-double "unaffected" criterion tested via ToJson because the shared test registry has no double-typed predicate.
- Review findings: inline review added the non-finite case to the `ToJson` XML `<exception>` doc.
- Hook / test time: format-all ~4 min again; commit hook ~5.5 min.
- Docs: docs/rulebuilder.md (same paragraph as ticket 36).
- Process friction: the format-all run before commit repeats what the hook does (the hook formats staged C# and runs the full build + tests); for agent runs, `format-all -- --check-only` only for Roslynator would save ~2 min per ticket.

## Ticket 49 (fix/parser-compilation-surfaces) - done
- Ticket accurate? Yes; reproduced. Small fix in `YamlTreeParser.Parse(string)`.
- Surprises: `YamlDataSource` reads only the first document by design (XML docs + a test); kept that, since the ticket is about rule input only. YAML Mark.Index points at the second document's root node, not the `---` line, so the span is the root node.
- TDD: red first. First assertion guessed the span at `---`; corrected to the root node after reading YamlDotNet positions.
- Review findings: none that changed the work.
- Hook / test time: format-all ~5 min, hook ~5 min.
- Docs: rule-formats.md (new one-paragraph rule), strong-k3 diagnostics.md TRE0014 row (third edit of that one row on this branch: a conflict hotspot for any parallel branch).
- Process friction: the TRE0014 row in strong-k3/specification/diagnostics.md is one very long table cell that every parser ticket edits; a bullet list under the table would merge better.

## Ticket 50 (fix/parser-compilation-surfaces) - done
- Ticket accurate? Yes; reproduced. A second case (variable reference of an argument named `args`) was in the ticket text and also mislabeled; both fixed.
- Surprises: none in logic. The fix mirrors TreeFormatReader's walk as a small role enum on the scan frame.
- TDD: red first (2 rows red), plus a green regression guard for nested `args` (operands, definition body) written before the change.
- Review findings: none changed the logic. Tooling did: CSharpier's layout of a nested ternary tripped SA1143 in `dotnet format`, which only surfaced in format-all (not in `dotnet build`), costing one extra 5-minute format-all cycle. Extracted an `OpenFrame` helper.
- Hook / test time: format-all ran twice (~10 min total); hook ~5 min.
- Docs: none needed; the spec pages already state the rule the code now follows (said so in the Resolution).
- Process friction: SA1143/CSharpier conflicts show only in the format gate. Suggest a fast `dotnet format --verify-no-changes --include <changed files>` step in the agent loop before the full format-all.

## Ticket 57 (fix/parser-compilation-surfaces) - done
- Ticket accurate? Yes; the grammar block still lacked `from(...)`. Checked the production against `DslParser.ParseVariableReference` before writing it.
- Surprises: the prose under Quantifiers writes `from(variable, "...")` with an unquoted first argument, while the parser requires a quoted source name (`from("line", ...)`, as the doctest below it shows). Not changed (outside the ticket); worth a small docs ticket.
- TDD: not applicable (doc-only, `doctest:skip` block); ran the documentation, doc-example, grammar and rule-text test classes instead.
- Review findings: none.
- Hook / test time: no C# changed, so format-all was skipped; the hook still ran build + full suite + doc-tests, but in 134 s (incremental build, warm caches) vs ~300 s for the C# tickets.
- Docs: docs/rule-text.md grammar block only.
- Process friction: a doc-only ticket still pays a full build + test hook. A docs-only fast path in the hook (skip build/test when only *.md under docs/ is staged, run doc-tests only) would help.

## Ticket 72 (fix/parser-compilation-surfaces) - done
- Ticket accurate? Yes; four diagnostics reproduced. The ticket's "plausible fix" (skip when the *next* token is `IN`) would not have fixed its own example (`INN items`: the next token is `items`). Used a token-shape rule instead: skip when `<token> identifier ,` follows, which leaves the omitted-keyword case alone.
- Surprises: the property is `QuantifierNode.CollectionName`, not `Collection` (one compile error in the red test).
- TDD: rewrote the existing misspelled-IN test to the single-diagnostic outcome (red), added a non-identifier row (`:`) and an assertion that `items` is the collection.
- Review findings: own review confirmed `FORALL(IN items, ...)` (missing variable) is unchanged.
- Hook / test time: format-all ~6 min this time (machine slower late in the run); hook ~5 min.
- Docs: rule-text.md Quantifiers paragraph (one sentence on the one-diagnostic recovery). The ticket had plain bullets, not checkboxes, so I stated the criteria as met in the Resolution.
- Process friction: tickets vary in acceptance-criteria format (checkboxes vs bullets); the brief says "tick checkboxes", which only works on half of them. A ticket template lint would help.

## Branch 1 fix/parser-compilation-surfaces (coordinator) - done, 7 commits, tip e947070
- All 7 tickets done; 3 of 7 ticket-proposed fixes were wrong or incomplete (13, 36, 72). Verifying against code first paid off. Ticket authors (the review) proposed fixes without running them.
- Cost: ~10 min per ticket, mostly format-all (4-6 min) plus the commit hook (~5 min). Suggest: fast `dotnet format --verify-no-changes --include <files>` pre-check; analyzer clashes (S4144, S127, SA1143 vs CSharpier) only show in format-all, not build.
- Testing gotcha: xUnit theory data turns a lone surrogate into U+FFFD. Add a line to the testing docs.
- Acceptance criteria inconsistent (checkboxes vs bullets) across tickets.
- Spawned follow-ups (not ticketed yet): 4 `JsonDocument.Parse(string)` call sites in TruthWeaver.Tool.Core still throw on a lone surrogate (ToolConfigurationFile, StubAssignment, EmbeddedRuleReader, FormatDetector); rule-text.md writes `from(variable, "...")` unquoted (for the docs branch, B4).
- Agent did the code review inline (no sub-agents available to it).

## Ticket 16 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes. Reproduced under tr-TR: `(?i)^id...$` vs "ID" answered False. Location right (RegexFlags 'i' case). The ticket's suggested "OR at RegexPredicates.cs:201/:218" would miss ValidateArguments with absent flags (starts from RegexOptions.None); put the always-on option in RegexFlags.TryParse and the ValidateArguments seed instead.
- Surprises: CultureInfo.GetCultureInfo("tr-TR") works in the container (ICU present), so the culture test is real, not vacuous.
- TDD: red first. The process-wide cache means the test pattern must be unique to the test, or another test could compile it first and mask the bug.
- Review findings: inline two-axis review; SA1515 (comment needs blank line above) only from dotnet format --include, not build.
- Hook: 293 s total (format --include on 3 files alone took 71 s).
- Docs: regex-ismatch.md, regex-isnotmatch.md, predicates.md. No lint friction.
- Process friction: `dotnet format --include` still loads the whole solution (~70 s); worth noting that it is not a fast check.

## Ticket 17 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes; padded GUID text answers IsGuid True (confirmed by a test). Decision was docs-only, which made "a test that fails before the fix" in the generic acceptance criteria impossible; added a pinning test that passes before and after and said so in the Resolution.
- Surprises: none.
- TDD: not applicable (no code change); pin test instead.
- Review findings: inline; added an Answers-table row on both pages besides the edge-case sentence.
- Hook: see secs below (docs staged, so doc-tests pass ran).
- Docs: type-isguid.md, type-isnotguid.md; K3Reference and DocumentationLint green locally before commit.
- Process friction: generic acceptance criteria ("test fails before the fix") conflict with docs-only Decisions; triage should drop that bullet when the Decision is "fix the docs".

## Ticket 29 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes; all three families (window, month, day) threw at both range ends. The Decision (compute precisely) was clear and right.
- Surprises: my own first "agrees with ToOffset" row (MaxValue - 5 h at +05:30) overflowed ToOffset in the test itself; picked rows where both sides are computable.
- TDD: red first (14 rows threw). InlineData cannot hold a DateTimeOffset, so rows use "min"/"max" tokens.
- Review findings: S6562 (DateTime without DateTimeKind) only from dotnet format --include / format-all, not build. Ran format-all --check-only before committing this time (~3+ min, all gates green).
- Hook: see secs in the log line below.
- Docs: Edge cases on datetime-isondayofweek/isinmonth/isintimewindow (twins have no Edge cases section, left alone).
- Process friction: the harness moved a foreground format-all to background after 180 s; launch it backgrounded from the start.

## Ticket 42 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes, all three findings reproduced (138 factories accepted a null selector, 122 accepted (NullBehavior)3, Add(schema, null!) registered). Location for F6.4 was one example; the real fix sites were 22 selector-capturing methods across 7 files (ScalarPredicateCore plus family builders).
- Surprises: Sonar S3928 fired because Resolve's parameter was `requested` but the exception named `nullBehavior`; renamed the parameter. Only format-all caught it (dotnet format stage), not build.
- TDD: red first, via a reflection test over every public generic factory, so new factories are covered automatically. First run collided keys on overloads (Func<T,long?> vs Func<T,decimal?>); needed generic-aware type names.
- Review findings: own Spec pass left SelectedValuePredicates (select/test) and `name` unchecked as out of scope; listed in Resolution.
- Hook: 328 s. format-all --check-only stopped at the first failing gate, so Roslynator did not run for this ticket; rerun planned at the end.
- Docs: predicates.md, strong-k3/predicates/README.md; 122+138 XML `<exception>` lines added with a scratch .cs script (no sed multi-condition support).
- Process friction: a file-based .cs script was the only practical way to do a conditional multi-line doc edit across 8 files; worth a shared "insert after matching doc line" helper in scripts/shared.

## Ticket 47 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes (two findings, same row). The neighbouring row (null element of an array query) also omitted its kind; named it too (same code path).
- Surprises: none; an existing fake-source test already pinned the mapping, but nothing pinned it end to end with JsonDataSource.
- TDD: docs-only; added an end-to-end pin test (null/object/array) that passes before and after.
- Review findings: inline; none changed the work.
- Hook: see secs line below.
- Docs: data-sources.md; DocumentationLint green locally.
- Process friction: none new.

## Ticket 61 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes, exactly as written (four defaults, two overloads each).
- Surprises: the docs already had the right labels, so no page changed.
- TDD: red first. SA1202 (public after private) from dotnet format --include because I appended tests after the private helpers; moved the block.
- Review findings: inline; checked fixtures/Tui catalog for the old label string (only method names matched).
- Hook: see secs line below.
- Docs: none.
- Process friction: none new; appending tests at the end of a test class is a trap when the class ends with private helpers.

## Ticket 62 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes. The index sentence was also wrong for IsEmpty and relatives (a null element makes a collection not empty); fixed in the same sentence.
- Surprises: none.
- TDD: docs-only; added a pin test covering every claim in the new sentence (passes before and after).
- Review findings: xUnit1051 flagged `default` as the CancellationToken argument (CancellationToken.None is accepted); only visible via dotnet format --include.
- Hook: see secs line below.
- Docs: strong-k3/predicates/README.md; K3Reference and DocumentationLint green locally.
- Process friction: none new.

## Ticket 63 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes; no validator, negative count compiled to a constant. Decision clear.
- Surprises: the ten factories shared the generic `Build`; a dedicated `CountPredicate` builder was cleaner than threading an optional validator through Build. Rewrote the ten call sites with a scratch .cs script.
- TDD: red first (30 rows). First test layout tripped SA1201 (property/field after methods), SA1110 (multi-line tuple type in a Dictionary generic; CSharpier vs StyleCop) and S8969 (repeated `!` on the same nullable). Fixed with a file-level tuple alias and a switch-based factory method.
- Review findings: inline; ran Roslynator separately (75 s, 0 diagnostics), which also covers ticket 42.
- Hook: see secs line below.
- Docs: ten collection-*hascount* pages (Arguments, Edge cases) and predicates.md; K3Reference ran in the hook.
- Process friction: format-all stops at the first failing gate, so a dotnet-format failure hides Roslynator results; a `--keep-going` flag would save a second 4-minute run.

## Ticket 64 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes; all four text arguments (offset, start, end, duration) accepted a trailing "\n". Decision (\z) fixed it in three regexes.
- Surprises: none; TimeZoneNamePattern also uses $ but only chooses the error wording for an already-rejected value, so left alone and noted.
- TDD: red first (4 rows), plus a green control test with the exact values.
- Review findings: inline; none changed the work.
- Hook: see secs line below.
- Docs: strong-k3/predicates/README.md (Fixed offsets).
- Process friction: none new.

## Ticket 65 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes, and incomplete: the same unconditional `Current.Value = parent` also dropped a still-open inner scope when the outer one was disposed first. The ticket's "only restore when current" fix alone would leave the disposed outer scope applying while the inner one is open; added a skip-disposed rule in Resolve too.
- Surprises: my first "outer disposed while inner open" test passed before the fix, for the wrong reason (the inner scope had been dropped and its value equalled the default). Rewrote it with distinguishable values.
- TDD: red first (after the rewrite). Lesson: pick test values where the buggy and fixed paths cannot coincide.
- Review findings: inline Spec pass found the second effect above; it changed the fix.
- Hook: see secs line below.
- Docs: predicates.md (scope disposal, in and out of order); no page described disposal before.
- Process friction: none new.

## Ticket 71 (fix/predicate-datasource-leaf) - done
- Ticket accurate? Yes; reproduced with two registries each binding "lines" to a different element projection. The ticket's "add a registry component the same way" was right for element sources; for root sources I deliberately kept a null registry (all rules of a PolicySet share one DataSources, so a registry component there would only add re-queries). Stated in the Resolution; owner may want to confirm.
- Surprises: the right registry is the one holding the collection binding (CollectionBinding's outerRegistry), not the evaluator's own registry; using `this.registry` would re-query an outer element source from a nested quantifier body.
- TDD: red first via PolicySet. Policies.Tests needed `using TruthWeaver.Building` for RuleBuilder.
- Review findings: inline Spec pass changed the registry choice (above).
- Hook: see secs line below. format-all --check-only green first time (~4 min).
- Docs: policies.md (Shared predicates). No data-sources.md change: its "each pair queried once" sentence is about one evaluation and stays true.
- Process friction: none new.

## Branch fix/predicate-datasource-leaf - summary
- 11 tickets, 11 commits, all done (no wontfix/skip). Hooks 240-330 s each; ~5 min per ticket of wall time was hook wait.
- Three tickets' proposed fixes were incomplete as written: 16 (the suggested OR site missed the no-flags validator path), 65 (dispose also dropped an open inner scope), 71 (which registry to key by). Docs-only tickets (17, 47, 62) had generic "test fails before the fix" criteria that cannot hold; pin tests used instead.

## Branch 2 fix/predicate-datasource-leaf (coordinator) - done, 11 commits, tip 439c1bc
- All 11 done. Incomplete proposed fixes again: 16, 65, 71. Docs-only Decisions (17, 47, 62) conflict with the generic "a test fails before the fix" acceptance bullet; pin tests used. Triage should drop that bullet when the Decision is "fix the docs".
- Analyzer findings (SA1515, S6562, S3928, SA1202, SA1201, SA1110, S8969, xUnit1051) show only in `dotnet format --include` / format-all, never `dotnet build`. format-all stops at the first failing gate (hid Roslynator once): a --keep-going flag would help.
- ~5 min hook wait per ticket (240-330 s). Backgrounding commit and reading code meanwhile (no edits) kept idle time down.
- Bulk conditional edits (260 XML doc lines, 22 selector guards, 10 call sites) needed scratch .cs scripts: a shared helper in scripts/shared would help.
- OWNER DECISION PENDING (ticket 71): root (non-element) sources carry no registry component in the SourceAnswers key because all rules in a PolicySet share one DataSources. Owner must confirm.
- For docs branch B4: diagnostics.md TRE0026 still shows only reversed bounds; negative HasCount count is now TRE0026. Also rule-text.md `from(variable, "...")` unquoted first arg.

## Ticket 19 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Doc findings yes (8 pages). The Coalesce Decision is internally inconsistent: it folds only the IEnumerable overload and leaves params, which breaks the pinned "array and list give the same rule" principle (rulebuilder.md, a consistency theory). Followed the Decision literally and documented the exception; owner may want params folded too.
- Surprises: and.md/or.md bullets were also stale about "params rejects"; ticket "Acceptance criteria" had a plain-bullet list plus the Agent Brief checkbox list.
- TDD: red first (2 failures) for Coalesce; docs-only parts have no test (K3Reference sync and doc-lint tests pass).
- Review: inline; added CHANGELOG entry (the earlier changelog line said Coalesce does not fold).
- Timings: narrow test classes ~20 s each.
- Process friction: tickets whose Decision conflicts with a documented invariant should be flagged in grilling before dispatch.

## Ticket 38 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Mostly. rule-text.md line number had moved (204 -> 214) and the page now already said rule text reads the letters; only the "words only" sentence was stale. ADR claims and diagnostic text as described.
- Surprises: the existing test pins the message prefix "'const' must be a JSON boolean or one of", so the first wording I wrote (", one of ... or one of ...") broke it; changed to a single list.
- TDD: red first (2 failing Fact tests). Message-text tests are cheap.
- Review: inline; none beyond scope. I had already done ticket 48's code in the tree and split it back out to keep commits per ticket.
- Hook: see ticket 19 (~5 min).
- Docs: rule-text.md, CHANGELOG, ADR-0005 in place. Ticket 38 says in-place ADR amendment, ticket 39 says appended errata; both touch the same ADR file, so 39 must be done after 38 is committed.

## Ticket 39 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Yes; all six findings reproduced (re-checked against code before writing the errata). Line numbers had drifted a little.
- Surprises: none. Ticket 38 (in-place) and 39 (appended errata) touch the same ADR; sequential commits avoided a conflict.
- TDD: none, comment plus ADR only; stated in the Resolution.
- Review: inline; kept errata to the ticket's list.
- Process friction: grep of `src` also hits bin/ XML docs (stale build output); suggest excluding bin/obj in the repo's grep guidance.

## Ticket 48 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Yes, exact. The text `null` also counts as null per IsNull, so `const: null` now reports `null` too (consistent with Describe).
- TDD: red first (2 failures), trivial fix. Had to split it out of ticket 38's working tree (I did it early); cheap.
- Review: inline, nothing changed. No docs page covers diagnostic Found text; CHANGELOG only.

## Ticket 53 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Mostly. "Same point as FaultBudget" is not construction (records are unvalidated); it is at each evaluate entry, and FaultBudget was checked in four places (two EvaluateAsync overloads, EvaluateManyAsync, ShadowReplay) with different code. Centralized in one internal method. Zero timeout is ambiguous in the Decision ("negative"); kept valid. The brief said "rejected below 1", which does not apply to a TimeSpan.
- Surprises: the exact CancelAfter upper limit is 4,294,967,294 ms, not int.MaxValue (probed with a scratch file-based app).
- TDD: red first; 3 of 10 rows already threw (CancelAfter) for single EvaluateAsync, so the real reds were the EvaluateMany timeouts and the element bound.
- Review: inline; xUnit1044 (non-serializable theory type) and SA1201/SA1202 ordering only show in dotnet format/build, fixed before commit.
- Docs: diagnostics.md, batch-evaluation.md, shadow-replay.md, XML docs. PolicySet is not validated upfront (the first rule's evaluation throws); not changed.

## Ticket 68 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Yes. The design choice (kind parameter vs one method per origin) interacts with "EventId unaffected": one method per origin would need new event ids (or duplicate ids, which the LoggerMessage generator flags), so I used one method with an enum parameter.
- Surprises: the shared test helper could not assert rendered text (only fields); added a rendered Message to LoggedCall via the formatter argument.
- TDD: red first for selector and limit; the predicate test passed first time and pins the unchanged text.
- Docs: no page covered the log line; added a short Logs section to observability.md.
- Review: inline; kept variable faults under the Predicate origin (not asked to change).

## Ticket 69 (fix/analysis-and-evaluation-leftovers) - done
- Ticket accurate? Yes; FaultBudget did have the same gap. Ticket 53 also edited the same XML docs (the validation sentences), so 53 before 69 was the right order and gave no conflict.
- TDD: none, XML doc comment only; build with analyzers is the check.
- Review: inline; kept docs/policies.md as a plain code-literal reference since XML docs cannot link to a docs file.
- Process friction: docs-only commits still pay the full ~5 min hook (full suite). A path-based hook shortcut for comment/doc-only diffs would help.

## Branch 3 fix/analysis-and-evaluation-leftovers (coordinator) - done, 7 commits, tip a12d590
- All 7 done. Doc-only commits still pay the ~5 min full-suite hook: a path-based shortcut for comment/doc-only diffs would save most of it (harness also backgrounds commands over 180 s).
- Decisions conflicting with a documented invariant (19) and ambiguous briefs (53: "below 1" for a TimeSpan; "same as FaultBudget" when it was checked in four places) should be settled in grilling before dispatch, not by the agent.
- OWNER QUESTIONS PENDING: (19) Coalesce(IEnumerable) now folds but Coalesce(params) does not, so Coalesce is the only operator where array and list give different rules, against the "array and list give the same rule" principle in docs/rulebuilder.md; folding params too is a small follow-up. (53) PolicySet does not validate options up front (first rule's evaluation throws); Timeout of exactly zero is allowed.
- Grep on src also hits stale bin/ XML docs.

## Ticket 59 (fix/reference-docs-drift) - done
- Ticket accurate? Yes, both findings reproduced. The ticket's F1.3 implied adding forall/exists to the read table; that would make `TryFromTreeFormat` return a canonical name with no OperatorDefinition, so they went into a separate suggestion list instead.
- Surprises: the outline code already had the quantifier label/description; moved it into `OperatorInfo.Describe` and made the outline call it (one home).
- TDD: red first (2 quantifier rows + equivalence row failed). The tree-typo test passed only after the fix, run together with the rest.
- Review: inline two-axis review. A first sketch of a second "outline matches Describe" test was a tautology; deleted.
- Timing: format-all failed once on SA1118 (multi-line ternary argument), so two full format runs (~4-5 min each). Hook ~5 min (doc-tests phase last). Shell wait commands over 180 s get auto-backgrounded, so waits need re-issuing.
- Docs: rulebuilder.md one sentence; also the two drifts (rule-text `from("line", ...)` prose lines 190/193 verified wrong, diagnostics TRE0026 negative HasCount verified) folded into this commit.
- Process friction: the ticket line numbers (e.g. 1171) are stale; run `dotnet format --verify-no-changes --include` early to catch SA findings before the 4-min format-all. Splitting two tickets sharing one test file mid-flight needs manual hunk surgery (no non-interactive `git add -p`); suggest dispatching tickets that touch the same file in separate bundles.

## Ticket 60 (fix/reference-docs-drift) - done
- Ticket accurate? Yes; one-word fix. Reference doc already said aliases.
- TDD: red first (description assertion). Single test in OperatorInfoTests.
- Hook ~5 min for a one-line source change; nothing else notable.

## Ticket 66 (fix/reference-docs-drift) - done
- Ticket accurate? Mostly. Line numbers stale (1171/2190/2244 vs 209/158/212). The proposed wording "Simplify and Canonicalize keep BETWEEN/NONE unless constant" was too broad: Canonicalize keeps both even with a constant (probed). Only Simplify qualified. A third unconditional claim (none.md:158 "leave NONE as NONE") also needed the exception.
- TDD: docs-only, pin tests that pass before and after (code was right). Probe test used to learn Canonicalize behavior, then replaced by a real pin.
- Hook ~5 min incl. doc-tests; K3Reference + DocumentationLint run beforehand were green.
- Friction: none beyond the usual wait/auto-background loop.

## Ticket 67 (fix/reference-docs-drift) - done
- Ticket accurate? Repro true; no Decision section, only the Agent Brief ("faulted or clearly distinct"). Followed it. An existing test deliberately asserted the sentence does NOT contain "faulted" (no recorded reason in git history), so this reverses a past implicit choice: flag to owner.
- Scope: also fixed TraceReasons.ForLeaf (Tool.Core) and the Explain XML summary; left AssignmentSession "The rule could not be evaluated." and CaseCoverage text (different states), CHANGELOG (history).
- TDD: red first on both tests (Explain, AssignmentSession). Needed `--filter-class` not `--filter-method` (the method filter ran 0 tests with a wildcard pattern).
- Timing: format-all ~4.5 min, hook ~5 min.
- Process friction: tickets should state an explicit Decision when they reverse an existing test assertion.

## Owner decisions (2026-11-01)
- Ticket 67: keep the new "faulted" wording and the reversed ExplainTests assertion. No change.
- Ticket 19: fold `Coalesce(params)` too, so array and list give the same rule. Follow-up ticket needed; remove the documented exception in docs/rulebuilder.md with it.
- Ticket 53: `PolicySet` validates evaluation options up front. Follow-up ticket needed.
- Ticket 53: a `Timeout` of exactly zero stays valid. No change.
- Scratch notes committed on fix/reference-docs-drift.

## Correction to the "doc-only commits pay the full suite" notes (2026-11-01)
- The hook already limits the `build` and `test` tasks to `*.cs`, `*.csproj`, `*.props`, `*.targets`, `*.slnx` and `global.json`. A commit that stages only `*.md` files runs the documentation tests only.
- The slow commits (tickets 60, 66, 69) staged a `.cs` file for an XML-doc comment, a string change or a test. A path filter cannot tell a comment-only `.cs` change from a logic change, and a comment can break the analyzers, so the hook stays as is.
- Retro follow-ups done: `format-all.cs --files`, the dispatch-brief section in `docs/agents/git.md`, a staged-only Agent Brief check in `validate-triage-labels`, and `AdrReferenceInStringTests`.
- The check for a ticket with open decisions is not automated. Ticket 67 had an Agent Brief, and wording ambiguity cannot be found mechanically. Grilling (rule 8) stays the control.
