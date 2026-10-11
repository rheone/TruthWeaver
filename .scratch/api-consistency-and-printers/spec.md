# API consistency, Try forms and more printers

**Status:** grilled 2026-10-10, ready for implementation

Source: the owner's grilling session of 2026-10-10 after the least-surprise-normalization run. Packages are unpublished (`1.0.0-dev`), so breaking changes are allowed and are recorded in `CHANGELOG.md` with a migration step.

## Decisions

| Topic | Decision |
| --- | --- |
| Twin naming | Form A, state or relation: `IsX` / `IsNotX` (equality, ordering, range, membership, dates, calendar, null and type tests). Form B, verb phrase: `Verb` / `NotVerb` (`Contains`, `ContainsAll`, `ContainsAny`, `StartsWith`, `EndsWith`, `SetEquals`). A twin is always the Kleene complement of its positive. |
| Equality | One `IsEqualTo` (and `IsNotEqualTo`) for string, numeric and scalar, with optional `ignoreCase` and `trim` arguments (default `false`). `EqualsConfigurable` merges into it (same defaults). `EqualsIgnoreCase` is removed: it is `ignoreCase: true`. |
| Ordering | `IsGreaterThan`, `IsGreaterThanOrEqualTo`, `IsLessThan`, `IsLessThanOrEqualTo`, with new `IsNot…` twins (each an exact complement). |
| Regex | `IsMatch` / `IsNotMatch` with an optional `flags` argument (`i m s n x`), checked at compile time. Removes `Matches`, `MatchesIgnoreCase` and their twins. |
| Counts | Five named predicates `HasCountEqualTo`, `HasCountGreaterThan`, `HasCountGreaterThanOrEqualTo`, `HasCountLessThan`, `HasCountLessThanOrEqualTo`, twins `NotHasCount…`. |
| Result shape of rewrites | `Simplify`, `Canonicalize`, `CompressToDerived` and the normal forms keep returning `CompilationResult` (carries the `TRE0033` warning). Supersedes "keep the split" in least-surprise-normalization ticket 29. |
| Try forms | Every `…OrThrow` member needs a `bool Try…(in, out [, out …])` sibling, and docs show the `Try` form first. Applies to everything that returns `CompilationResult`: compile, rewrites, normal forms. An architecture test enforces the sibling rule. |
| Pragma | Remove the `VSTHRD002` suppression in the doc checker by making it async. |
| Symbol printers | `OperatorStyle` on `PrintRuleText` (round-trippable symbols only; `CStyle` rejected there). A `CStyle` dialect on `PrintEquation`. |
| Constants | A constant style: `Word` (today), `Letters` (T, F, U; default for the strict-logic variant), `LogicSymbols` (⊤, ⊥, U). Each constant also accepts an override string. Do not use `1`/`0`/`½` (the ordering must not read as numeric). |
| New formats | Equation dialects MathML, Typst and plain ASCII symbols; printers for rich HTML and Graphviz DOT; width-aware pretty `PrintRuleText`. Truth-table printing is not planned. |
| Writers | Every printer gets a `TextWriter` overload that writes the same text as the string form. |
| Docs | Prose pass (`humanizer`, `voice-of-robert`) on `docs/rewriting-rules.md`, then split its worked examples into a linked page. |

## Tickets

| Ticket | Change | Blocked by | Status |
| --- | --- | --- | --- |
| [01](issues/01-predicate-naming-guide-and-renames.md) | Predicate naming guide and renames | None | ready-for-agent |
| [02](issues/02-regex-ismatch-with-flags.md) | Regex `IsMatch` with `flags` | 01 | ready-for-agent |
| [03](issues/03-hascount-predicates.md) | `HasCount…` predicates | 02 | ready-for-agent |
| [04](issues/04-try-forms-and-try-first-convention.md) | Try forms and the Try-first convention | None | ready-for-agent |
| [05](issues/05-remove-vsthrd002-pragma.md) | Remove the VSTHRD002 pragma | None | ready-for-agent |
| [06](issues/06-operatorstyle-on-rule-text-and-cstyle-equation.md) | `OperatorStyle` on `PrintRuleText`, `CStyle` equation dialect | None | ready-for-agent |
| [07](issues/07-constant-style-and-override-strings.md) | Constant style and override strings | 06 | ready-for-agent |
| [08](issues/08-mathml-typst-ascii-equation-dialects.md) | MathML, Typst and plain-ASCII dialects | 06, 07 | ready-for-agent |
| [09](issues/09-rich-html-and-graphviz-dot-printers.md) | Rich HTML and Graphviz DOT printers | 07 | ready-for-agent |
| [10](issues/10-width-aware-pretty-rule-text.md) | Width-aware pretty rule text | 06 | ready-for-agent |
| [11](issues/11-rewriting-rules-prose-pass-and-split.md) | Prose pass and split of `rewriting-rules.md` | 04 | ready-for-agent |
| [12](issues/12-textwriter-overloads-on-printers.md) | `TextWriter` overloads on every printer | 06, 08, 09, 10 | ready-for-agent |

Suggested run order, one bundle at a time: 05, 04, 01, 02, 03, 06, 07, 08, 09, 10, 12, 11. The predicate tickets (01 to 03) share the catalog and its docs; the printer tickets (06 to 10, 12) share the `Printing` folder.
