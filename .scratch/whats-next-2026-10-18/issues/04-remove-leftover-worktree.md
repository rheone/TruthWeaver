# 04: Remove the leftover worktree

**What to build:** The worktree `.claude/worktrees/agent-adbd2e157a7ac66b5` is gone, after a check that it holds no unmerged work. `DocumentationLint` scans `.claude/worktrees`, so a leftover worktree can break the test run.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] The worktree's branch and uncommitted changes are inspected, and anything unmerged is reported to the owner before removal.
- [x] The worktree and its branch are removed.
- [x] `git worktree list` shows only the main working tree.
