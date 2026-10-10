# 01: Collapse nested findings

**What to build:** An author who writes one redundant construct sees one finding, not one per enclosing lint.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] When a finding's replacement already removes the subtree that holds a second finding, only the outermost finding is reported
- [ ] Independent findings in separate branches are all still reported
- [ ] Order stays "outermost construct first"
- [ ] A test covers an `If` whose condition holds a redundant inspection
- [ ] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

Open detail: decide whether "already explains" means the inner node is absent from the outer replacement. Confirm with the owner before coding.

See also [spec](../spec.md).
