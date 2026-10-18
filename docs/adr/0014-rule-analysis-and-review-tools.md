# ADR-0014: Rule analysis and review tools

## Status

Accepted (2026-10-17). Records six features that shipped without an ADR, as one group, as the owner decided on
2026-10-17 in `.scratch/main-review-2026-10-17/issues/08-shipped-surface-without-adr.md`: `RuleCounterfactual` (with its
internal `ChangeSetSearch`), `RuleImplication` and `RuleRelation`, `Linter` (the opt-in lint rules), `RuleCoverage` and
`RuleDiff`. It changes no code. It amends nothing. `RuleEquivalence` and `RuleOverlap` have their own records
([ADR-0005](0005-strong-k3-language-surface.md) decision 17 and [ADR-0009](0009-overlap-and-conflict-definitions.md)).

## Context

After the Strong K3 work, the analyzer could answer exact questions about a compiled rule: is it constant, are two rules
equivalent. Hosts then needed answers one step further out: what must change for this denied decision to pass, does the
new version of a rule let in more people than the old one, which part of a rule is dead in production, and what
changed between two versions. Each tool was built on the same analyzer, with its own small decisions, and none was
recorded. The tools share a set of rules that a user should be able to rely on; this ADR states them once.

## Decision

### 1. One reasoning model for every analysis tool

`RuleCounterfactual`, `RuleImplication`, `RuleOverlap`, `RuleEquivalence`, the `Linter` and the semantic part of
`RuleDiff` all use the analyzer's dual-rail BDD (ADR-0005 decision 17): each term has independent "is `True`" and "is
`Unknown`" variables, so every assignment of `{True, False, Unknown}` to the terms is a valid state and the three
values stay distinct. The rules that follow from this are the same everywhere:

- **Exact, not sampled.** The result holds for every assignment, and no truth table is enumerated.
- **No predicate runs.** The tools read the compiled tree (and, for coverage, recorded decisions). They never call a
  predicate or a data source, so they are safe to run on a rule whose predicates are not registered in the process.
- **Terms are opaque and independent.** Two terms are the same variable only when their term identity is equal. The
  tools cannot know that `ageAtLeast(n: 21)` implies `ageAtLeast(n: 18)`, so an example assignment can name values that
  cannot occur together in the application. A *proof* ("no assignment reaches this") holds for every real context; an
  *example* is a candidate only.
- **Not an evaluation.** The tools ignore evaluation order, short-circuiting and faults.
- **A cap, and an honest answer past it.** When a question has more distinct terms than `MaxAnalysisTerms` (default 20,
  [ADR-0002](0002-evaluation-semantics.md)), the tool does not run and returns the named outcome `Undecided` with a
  reason. It never reports a guess, and `Undecided` is never confused with the truth value `Unknown`.
- **Satisfied means `True`.** Where a tool compares "who passes", an assignment satisfies a rule when the rule is
  `True` for it, the same reading as `Decision.IsSatisfied` (fail-closed, [ADR-0001](0001-kleene-failure-model.md)).
  `False` and `Unknown` do not satisfy.

### 2. `RuleCounterfactual`: the smallest changes that give a rule a target value

`RuleCounterfactual.FindChanges` answers "what would make this rule `True`?" from the current facts (a dictionary of
term values, or the term values of a `Decision`). A change sets one term to `True`, `False` or `Unknown`. The sets are
minimal (no smaller part of a set gives the target), they come fewest changes first, and at most
`CounterfactualOptions.MaxChangeSets` (default 10) are returned. `FixedTerms` names terms that must not change.
A term without a fact can take any value, and a set that does not name it must give the target for all three values.
The result says whether the list is complete (`IsComplete` is `false` when the cap, or the internal work limit of the
search, stopped it). The outcomes are `Found`, `NoChangeNeeded`, `NonePossible` and `Undecided`. The search class
`ChangeSetSearch` is internal: the public surface is `RuleCounterfactual`.

The result is a "what to fix" list, not a promise. A change set is a set of term values, and a host decides whether
a term can really be changed.

### 3. `RuleImplication` and `RuleRelation`: is a new version looser or tighter

`RuleImplication.Implies` decides whether every assignment that satisfies the first rule also satisfies the second.
`RuleImplication.Compare` places an "after" rule against a "before" rule and returns a `RuleRelation`: `Equivalent`
(the same assignments satisfy both), `Tighter`, `Looser`, `Overlapping`, `Disjoint` or `Undecided`. The comparison is
about where each rule is `True`, so two rules can be `Equivalent` here and differ in where one is `False` and the
other `Unknown`; `RuleEquivalence` compares every value. A rule that is never `True` is `Tighter` than any rule that is
sometimes `True`. "Looser" and "tighter" are about who is let in, which is the question a rule change review asks.

### 4. `Linter`: opt-in, informational, K3-equivalent only

The lint rules (`LintRules`, a flags enum on `CompilerOptions.Lints`) run after the analyzer. The default is `None`, so a rule
that compiled clean before keeps compiling clean. Each finding is an `Info` diagnostic with a `Replacement` suggestion
that holds the simpler rule text and a message that says why it means the same. A lint fires only when the replacement
is K3-equivalent for every assignment, never on a two-valued intuition: `a AND a` is flagged, `a XOR a` is not (it is
`Unknown` when `a` is, not `False`), and `a OR NOT a` is not a tautology. A lint never changes the compiled rule. A
finding has no source span, because an expression node does not remember where it was written; a finding that sits
inside another finding links to the nearest enclosing one (`Diagnostic.EnclosedBy`), and none is dropped. `Linter` is
internal. The public surface is the flag set, the diagnostic codes and the suggestion.

### 5. `RuleCoverage`: dead branches and unreliable data, from recorded decisions

`RuleCoverage.FromDecisions` (and the async form that consumes `EvaluateManyAsync`, [ADR-0012](0012-batch-evaluation.md))
reads the trace tree of each decision and counts, for each term and node: how often it was evaluated, its `True`,
`False` and `Unknown` counts, its faults and how often it decided the result. It calls no predicate and does not keep a
decision after it is counted. A node decides a decision when a different value at that node, with every other
evaluated node kept and every skipped node read as `Unknown`, changes the result of the rule. In short-circuit mode a
skipped operand cannot decide; exhaustive mode gives every reachable term a value in every decision.
A decision with no trace tree, or with a trace tree that does not match the rule, is rejected with an
`ArgumentException`, not counted. A quantifier body is folded onto one node, and the terms of a body are not in the
list of terms of the rule ([ADR-0010](0010-forall-exists-quantifiers.md) decision 8). For a node inside a body,
"decides" means that the change flips the body's own result for that element, not the result of the quantifier or of
the rule; `docs/rule-coverage.md` states this. The main review lists open points about quantifier counting, which this
ADR does not settle.

### 6. `RuleDiff`: where two versions differ, and whether the meaning moved

`RuleDiff.Compare` returns the structural entries (added, removed and changed operator, term and constant nodes, each
located by its operand-index path and carrying the `OutlineNode` of the node), plus two semantic answers:
`PreservesMeaning` (the K3 equivalence of the two rules, `null` when `Undecided`) and `Relation` (a `RuleRelation`).
Identical structure answers `true` and `Equivalent` without building a BDD. `maxAnalysisTerms` controls only the cap of
those two answers; the structural entries are always complete.

## Consequences

- The analysis pages must state the assumptions of section 1 (opaque independent terms, `Undecided`, "no predicate
  runs") where a user reads a result. `docs/counterfactuals.md` states them for `RuleCounterfactual`; the pages of
  the other tools must not contradict them.
- A tool that is added to this family uses the dual-rail BDD, the `MaxAnalysisTerms` cap and an `Undecided`
  outcome, or it gives its reason for not doing so in a new ADR.
- Every tool in this family reads a compiled rule or a recorded decision. None of them evaluates a rule, so a change to
  evaluation semantics needs no change to them, except where the analyzer encodes the same semantics (ADR-0005
  decision 17).
- The linter's "K3-equivalent only" rule means it reports less than a two-valued linter would. That is the intended
  trade: a finding is a statement the user can act on without changing the rule's value.
- The tools report a cap or a limit in their result (`Undecided`, `IsComplete`, a `null` `PreservesMeaning`) instead of
  throwing, so a caller must read it; a caller that ignores it can show a partial list as complete.

## Related

- [ADR-0001: Kleene failure model](0001-kleene-failure-model.md)
- [ADR-0002: Evaluation semantics](0002-evaluation-semantics.md)
- [ADR-0005: Strong K3 language surface](0005-strong-k3-language-surface.md)
- [ADR-0009: Overlap and conflict definitions](0009-overlap-and-conflict-definitions.md)
- [ADR-0010: FORALL and EXISTS over collections](0010-forall-exists-quantifiers.md)
- `docs/counterfactuals.md`, `docs/rule-coverage.md`
