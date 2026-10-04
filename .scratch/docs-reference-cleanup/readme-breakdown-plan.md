# Plan: audit of the other in-scope Markdown files and the root README breakdown

Prepared for ticket 12 of the docs-reference-cleanup effort. It changes no file. The owner approves or amends it before any file is edited.

## 1. Audit of the in-scope files

The documentation standard is in `CLAUDE.md`. `DocumentationLintBaseline` lists the files that do not meet it yet.

| File | Lines | Em dashes | ADR links | "ticket" | Doctests | Finding |
| --- | --- | --- | --- | --- | --- | --- |
| `README.md` | 2,549 | 117 | 29 | 2 | 23 | Too large. Repeats the K3 reference (operators, grammar, truth tables, glossary-style tables). Contains reference material for the compiler, the rule formats and the predicates that belongs in separate pages. |
| `CONTEXT.md` | 400 | 19 | 18 | 0 | 2 | Engine vocabulary and conceptual model. Carries a "superseded" note and ADR citations. Its vocabulary table overlaps `docs/glossary.md`. |
| `docs/data-sources.md` | 305 | 0 | 1 | 0 | 4 | Close to the standard. One ADR link to remove. |
| `AGENTS.md` | 17 | 2 | 0 | 0 | 0 | Two em dashes. |
| `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md` | 102 | 0 | 1 | 1 | 0 | A generated results file that the README links to. It mentions a ticket and an ADR. |
| `docs/agents/*.md` | 81 | 0 | 6 | 5 | 0 | Not in scope: no root file links to them with a Markdown link. They are agent files. An ADR link is correct there. |

## 2. Target shape of the root README

The README becomes a 150 to 200 line overview: what the project is, requirements, getting started, the package list, a feature summary, a documentation map and the license. Each other section moves to a page under `docs/` or is replaced by a link, as the table shows. Nothing that the K3 reference states is repeated.

| Section of `README.md` | Lines | Destination |
| --- | --- | --- |
| Title, introduction, badges | 1 to 58 | Stays. The table of contents shrinks to the remaining sections. |
| What it is (and isn't), Requirements, Getting started | 59 to 149 | Stays. |
| A tour of the codebase | 150 to 244 | `docs/architecture.md` |
| Packages | 245 to 301 | Stays as a short package list. Detail moves to `docs/packages.md`. |
| Features | 302 to 391 | Stays as a summary of about 15 lines that links to the pages below. |
| Operators (symbols, grammar, precedence, delimiters, whitespace, arity, all operators, connectives, Collapse) | 392 to 705 | Deleted, except for the EBNF grammar, the `GroupingStyle` and `RuleText.NormalizeWhitespace` API text and the case rules, which move to `docs/rule-text.md`. Everything else is in `docs/strong-k3/` (see section 3). |
| Rewriting rules, Rule equivalence | 706 to 979 | `docs/rewriting-rules.md`. Where a K3 page already states a fact, the new page links to it. |
| Choosing a rule format | 980 to 1010 | `docs/rule-formats.md` |
| Predicate types | 1011 to 1324 | `docs/predicates.md`. It links to `docs/data-sources.md` for variable references. |
| Examples | 1325 to 1834 | `docs/examples.md` |
| Building rules programmatically (converting formats, `RuleBuilder`, outline, diagram) | 1835 to 2007 | Converting formats joins `docs/rule-formats.md`. The rest becomes `docs/rulebuilder.md`. |
| Evaluation flow | 2008 to 2044 | The class diagram moves to `docs/architecture.md`. The behavior is in `docs/strong-k3/specification/evaluation.md`. |
| Compilation pipeline | 2045 to 2076 | `docs/architecture.md` |
| Reading diagnostics (API use, lint rules, JSON and YAML rules) | 2077 to 2228 | `docs/diagnostics.md`. The code catalog is in `docs/strong-k3/specification/diagnostics.md`. |
| Benchmarks | 2229 to 2270 | `docs/benchmarks.md` |
| Glossary | 2271 to 2274 | Stays as a link to `docs/glossary.md`. |
| Appendix: Truth tables | 2275 to 2522 | Deleted. Every table is on its Operation page in `docs/strong-k3/`, where the tests verify it. |
| Design documents | 2523 to 2543 | Removed. `docs/adr/` is the index of decisions, and `CONTEXT.md` keeps its own list. |
| License | 2544 to 2549 | Stays. |

A new "Documentation" section in the README lists each page with one sentence. It links to the K3 reference and to `docs/glossary.md`.

## 3. Content that the K3 reference already covers

| README content | K3 page |
| --- | --- |
| Symbol notation, order of operations, grouping delimiters (rules), arity table | `specification/syntax.md` |
| "All operators" table | `specification/operations.md` and each Operation page |
| Strong Kleene connectives and external operators | `specification/semantics.md` |
| Collapse and Project | `result-transformations/` |
| Truth-table appendix | Operation pages |
| Diagnostic codes | `specification/diagnostics.md` |
| Evaluation behavior | `specification/evaluation.md` |

## 4. Constraints that the move must respect

- **Doctests.** `DocExampleTests` checks `README.md`, `CONTEXT.md` and `docs/data-sources.md` by name. The 23 doctest markers in the README move with their blocks. Each new page that holds a marker is added to the `[InlineData]` list and to `docs/doc-examples.md`. A `rule` marker must come before the `json`, `yaml`, `tree` and `mermaid` blocks that use its ID, in the same file. The `worked` chain (rule, JSON, YAML, `RuleBuilder` tree and diagram) therefore moves to `docs/examples.md` as one unit, with `ex1` to `ex7b`. `ageVariable` moves with the predicate text. The two `diagnostics-json` markers and `diagnostics-dsl` move with the diagnostics text. An untagged `text`, `json`, `yaml`, `mermaid` or `ebnf` block fails the test, so every block that stays or moves keeps its marker.
- **Links.** The K3 checker verifies links only under `docs/strong-k3/`. A move can leave a broken link elsewhere. The first ticket of the second pass extends `DocumentationLint` to check relative links and heading anchors in every in-scope file, with the same code that the K3 checker uses. The README table of contents and any `README.md#...` anchors are updated in the same ticket as the move.
- **Standard.** Each new page follows `CLAUDE.md`: present tense, ASD-STE100, no ADR or ticket links, no em dashes. Each page leaves the baseline in the commit that creates it.
- **Line endings.** New and moved files are CRLF.

## 5. Other files in the second pass

- `CONTEXT.md`: remove the "superseded" note and the ADR citations that explain history. Keep the vocabulary and the conceptual model. Link to `docs/glossary.md` instead of repeating the vocabulary. The navigation list at the end keeps its ADR links, because the standard allows them in the four root files.
- `docs/data-sources.md`: remove the ADR link and state its rule directly.
- `AGENTS.md`: replace the two em dashes.
- `baseline-results.md`: see decision 2.

## 6. Second-pass tickets

Tickets 13 to 24 in `issues/`. Tickets 16 to 21 all edit `README.md`, so they run one after another and each updates the table of contents. Tickets 13, 14 and 15 have no blockers and can start at once.

| # | Ticket | Blocked by |
| --- | --- | --- |
| 13 | Check links and anchors in every in-scope file | None |
| 14 | Put `docs/agents/` on the stop list | None |
| 15 | Fix the benchmark generator wording and regenerate the results | None |
| 16 | Move the architecture and packages material | 13 |
| 17 | Move rule text, rule formats and `RuleBuilder`; delete the Operators section | 16 |
| 18 | Move the rewriting rules | 17 |
| 19 | Move the predicate text | 18 |
| 20 | Move the examples with their doctests | 19 |
| 21 | Move the diagnostics and benchmarks text | 15, 20 |
| 22 | Delete the truth-table appendix and the design documents list; add the Documentation section | 21 |
| 23 | Clean `CONTEXT.md`, `docs/data-sources.md` and `AGENTS.md` | 13 |
| 24 | Empty the lint baseline and run the full validation list | 14, 15, 22, 23 |

## 7. Decisions

The owner settled these on 2026-10-04.

1. The EBNF grammar lives in `docs/rule-text.md`, with the `GroupingStyle` and `RuleText.NormalizeWhitespace` text and the case rules. The EBNF block keeps its `doctest:skip` marker, and the page joins the doctest list.
2. The benchmark generator is changed so the file it writes passes the lint. The results file is regenerated and leaves the baseline.
3. `docs/agents/` joins the stop list in `DocumentationLint` and in the `CLAUDE.md` sentence.
4. The ten page names in section 2 stay as proposed.
