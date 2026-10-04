# 28: Decide whether AAA and NSubstitute are enforced or the rules amended

**What to build:** A decision, then the follow-through. CLAUDE.md says tests follow Arrange / Act / Assert and that NSubstitute is preferred for test doubles. The review found that only 3 of 73 changed test files have Arrange markers, the branch adds a single `Substitute.For`, and most doubles are the hand-rolled `FakePredicates` and `TestPredicates` (`FakePredicates` is a deliberate package). The existing style also predates the branch. Recommendation: amend CLAUDE.md to describe how the repo actually tests (AAA by shape, not by comment; `FakePredicates` as the default double, NSubstitute for other seams). The owner decides; if enforcement is chosen, split a migration ticket.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The owner picks: amend the rules, or enforce them
- [ ] If amended, CLAUDE.md Testing and Development rules describe the real convention, and AGENTS.md is updated to match
- [ ] If enforced, a migration ticket is written with a scope sized to one context window per batch
- [ ] The decision is recorded in the issues log

Source: review of PR #4, Standards axis, hard violation 2 and judgement call on NSubstitute.

## Comments

- 2026-10-03: Owner chose to amend the rules to describe the real convention. Work tracked by k3-followups 35.
