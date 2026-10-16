# ADR-0009: Joint outcome, overlap and conflict definitions

## Status

Accepted (2026-10-16). Records the owner decisions from the grilling session on
`.scratch/feature-backlog-2026-10-11/notes/21-overlap-conflict-design.md` (spike output, 2026-10-11), answering
that note's open questions Q1 through Q9. Ticket:
[21b-1](../../.scratch/feature-backlog-2026-10-11/issues/21b-1-overlap-conflict-adr.md). Parent:
[21](../../.scratch/feature-backlog-2026-10-11/issues/21-spike-overlap-and-conflict-analysis.md). This ADR defines
terms only; it does not change any shipped API. The first implementing ticket (21b-2, `RuleOverlap.Compare`) and
later ticket 07 (policy sets and combining algorithms) build on these definitions without redeciding them.

## Context

Ticket 13 answers "is rule A looser or tighter than rule B" for one rule compared with a new version of itself
(`RuleImplication.Compare`, `RuleEquivalence`). Authors of a *group* of peer rules — a permission rule per action, a
feature flag per audience — ask two further questions that ticket 13 does not answer: "can these two rules ever both
apply" and "can these two rules ever disagree". The design note above explores what the engine can honestly claim
about those questions, given that a rule's terms are opaque, independent predicates (`CONTEXT.md`, Term identity)
and that TruthWeaver evaluates Strong Kleene (K3), not two-valued, logic.

The note proposes a first slice, `RuleOverlap.Compare`, built on the same dual-rail BDD `RuleImplication.Compare`
already uses, and lists nine open questions for the owner. This ADR records the owner's answers as the vocabulary
and shape that ticket 21b-2 is built against, so the terms exist before any library code changes.

## Decision

1. **Joint outcome.** For two rules `A` and `B` sharing the same universe of assignments (a map from every distinct
   term of either rule to `True`, `False` or `Unknown` — the same space `RuleEquivalence` and `RuleImplication`
   already use), the joint outcome of an assignment `σ` is the pair `(A(σ), B(σ))`. A pair is **reachable** when
   some `σ` produces it. There are nine possible pairs; `RuleOverlap.Compare` (ticket 21b-2) reports which are
   reachable, each with one example assignment.

2. **Overlap.** `A` and `B` **overlap** when the pair `(True, True)` is reachable: some context satisfies both
   rules at once. This is ticket 13's `Shared` flag, restated as one cell of the joint-outcome table.

3. **Conflict is the satisfaction reading (Q2, option 2), not the design note's recommended strict reading.**
   `A` and `B` **conflict** when one rule is `True` and the other is not `True` for the same assignment: the pair
   is `(True, False)`, `(True, Unknown)`, `(False, True)` or `(Unknown, True)`. A pair of two `Unknown` values is
   not a conflict, because neither rule is satisfied, so there is nothing for the two to disagree about. This
   matches `Decision.IsSatisfied` and ticket 13's `SatisfiesOnlyBefore`/`SatisfiesOnlyAfter`, which already read
   "not `True`" as the failure state a host acts on (a permission or feature-flag check that comes back `Unknown`
   is treated the same as one that comes back `False`). The design note's Reading A (strict: `True`/`False` only)
   remains a documented, narrower question — "do the two rules ever give a *definite* and opposite answer" — but it
   is not what the shipped API calls a conflict. Reading C (any off-diagonal pair) is not called a conflict either,
   for the reason the note gives: once any term can be `Unknown` (a fault reaches every term), `(True, Unknown)` is
   reachable for nearly every pair of rules that share no structure, and a word that fires that often stops being
   useful to an author.

4. **Proof versus candidate.** An **unreachable** pair ("A and B never overlap", "A and B never conflict") is a
   proof for every real application context, provided the predicate-author contract holds (the same term answers
   the same way within one evaluation): every real context produces some assignment, so no context can reach a pair
   that no assignment reaches. A **reachable** pair, with its example, is a candidate only: because terms are
   opaque and independent (`CONTEXT.md`, Term identity), the example assignment can be impossible in the
   application (`ageAtLeast(n: 21) = True` with `ageAtLeast(n: 18) = False`). Documentation and the API's own member
   comments state this distinction wherever a result is reported, because a candidate example reads like a fact if
   it is not labelled.

5. **Scope for this ADR: pairwise first (Q1, option 1).** `RuleOverlap.Compare(first, second)` for two peer rules is
   the only slice this ADR authorizes building against these definitions. Set-level questions (redundancy in a rule
   set, shadowing in an ordered list) are deferred to a later slice, because they need a combining reading
   TruthWeaver does not define yet (ticket 07, on hold) and no BDD node budget exists yet (Q7). Deferred, not
   rejected:
   - **Result shape (Q3, option 1).** The reachable pairs, each with an example, plus `CanOverlap` and
     `CanConflict` convenience members that are `false` whenever the overall result is undecided — a caller checks
     the outcome first, rather than treating a `false` convenience member as a proved "never".
   - **Assumptions (Q6, option 1).** A caller-supplied fact between terms (`ageAtLeast(21) IMPLIES ageAtLeast(18)`)
     that narrows which assignments are considered is a later, optional argument on the comparison APIs, added
     after the pairwise slice ships, not in it.
   - **BDD node budget (Q7, option 1).** An internal node limit on the BDD builder, bounding analysis time
     independently of the existing term cap, is added in the set-level slice and then applied to the pairwise
     check (and to ticket 13's existing comparisons) as well.
   - **Set identity (Q8, option 1).** When a set-level report exists, a rule in a set is identified by its index in
     the input list; TruthWeaver has no rule-name concept for a host to key by (`CONTEXT.md`: a name or version
     around a rule belongs to the application).
   - **Shadowing (Q4, option 1).** Whether an earlier rule that is `Unknown` blocks a later one in a "first match
     wins" reading depends on what ticket 07 decides a combining algorithm does with an `Unknown` rule. Shadowing
     is not defined by this ADR and waits for that decision.
   - **Redundancy (Q5, option 1).** When a set-level report exists, it states redundancy per rule ("removable given
     every other rule currently in the set") and additionally proposes one set of rules that can be removed
     together, found by a greedy walk from the end of the list. It does not attempt the exact minimum removable
     set.

## Considered options

- **Conflict as the strict reading (design note Reading A, `True`/`False` only).** This is the reading with the
  clean property the note proves: for connective-only rules, a definite conflict is reachable over K3 assignments
  if and only if it is reachable over two-valued assignments, so an author can reason about it without thinking
  about `Unknown` at all. Not chosen as the default: the owner judged "one side is satisfied and the other is not"
  closer to what a host actually checks (`Decision.IsSatisfied`), and ticket 13 already ships that reading under
  `SatisfiesOnlyBefore`/`After`. The strict reading is still a well-defined question the table answers (the
  `(True, False)` and `(False, True)` cells); it is simply not the one the word "conflict" names.
- **Conflict as any disagreement (design note Reading C).** Rejected, as the note argues: it coincides with "not
  equivalent" (`RuleEquivalence`) and loses meaning once any term can be `Unknown`.
- **Pairwise and set-level analysis together (Q1, option 2).** Rejected for now: ticket 07's combining semantics
  and a node budget do not exist yet, so a set-level answer has no time bound (design note, section 5) and no
  agreed meaning for an `Unknown` rule in the set.
- **`bool?` for `CanOverlap`/`CanConflict` (Q3, option 2).** Rejected: it invites a caller to treat `null` and
  `false` the same by accident. A plain `bool` that is `false` when undecided, next to an `Outcome` the caller must
  check first, states the fail-closed reading explicitly instead of leaning on the type system to carry it.
- **A single relation enum with conflict variants (Q3, option 3).** Rejected: the nine-cell table is the one honest
  statement of what the analysis knows; collapsing it into a named relation ahead of time would hide cells a caller
  might want (for example, which `Unknown` pairs are reachable, needed to understand why a candidate example looks
  the way it does).

## Consequences

- `CONTEXT.md` and `docs/glossary.md` gain entries for **joint outcome**, **overlap** and **conflict**, defined
  exactly as decisions 1 through 3 state them (this ADR's companion change).
- Ticket 21b-2 (`RuleOverlap.Compare`, `RuleOverlapResult`, `JointOutcome`) is built against decisions 1 through 4
  and the Q3 result shape; it does not reopen the conflict reading.
- **Conflict reporting is noisy under the chosen reading**, more so than under the rejected strict reading: because
  `(True, Unknown)` and `(Unknown, True)` both count, `CanConflict` will be `true` for most pairs of rules that
  share no asserted relationship between their terms, for the same structural reason the design note gives for
  overlap (section 3: 763 of 780 random pairs over 20 terms overlap). A report that leads with `CanConflict` is
  lower-value than one that leads with the proofs — "these two never overlap", "these two never conflict", "this
  one is covered by that one" — and the implementing ticket's docs and report shaping should say so.
  `IsUnknown`/`IsFalse`/etc. terms make this worse still, because they can make `(True, Unknown)` reachable through
  a term's own external-operator logic rather than through two independent predicates.
  Documentation states the strict reading as the question "do these two rules ever give opposite *definite*
  answers", separately available as two named cells of the table, for an author who wants that narrower,
  less-noisy question.
- Ticket 07 (policy sets and combining algorithms), when it resumes, must not redefine **conflict**,
  **overlap** or **joint outcome**; it builds set-level analysis (shadowing, redundancy) on top of these, as
  decision 5 already anticipates.
- No change to any shipped type, method or diagnostic. This ADR only fixes vocabulary and the API shape ticket
  21b-2 will implement against; `CHANGELOG.md` is not updated for it because there is no behavior to announce yet.

## Open questions for the owner

None. The nine questions the design note raised (Q1 through Q9) are all answered by decisions 1 through 5 above.
