# Recipes

Named shapes for common requests. Each is a template for the task graph in step 2. Recommend one in the plan when the request fits, and expand it into tasks.

| Recipe | Fits | Graph |
| --- | --- | --- |
| **Fan-out** | "for each" item, the same work per item | one task per item, no edges (batch) |
| **Pipeline** | ordered steps, each using the last result | each task after the previous (chain) |
| **Map-reduce** | many things to read, one answer to build | read-only task per item, then one **reduce** task after all of them, fed their `Handoff` lines |
| **Review loop** | work that must be checked | work task, then a **review** task after it, then a **fix** task after the review only when the review fails |
| **Speculative** | a design choice with several viable approaches | up to 3 tasks that attempt the same goal differently in worktrees, then a **judge** task after all of them |

## Rules

- Map tasks and reviews are read-only: tell the agent not to modify files.
- A review checks the work against its done-when and the diff, and runs in a fresh bundle, never the one that did the work.
- A reduce or judge task is tier judgment; the map tasks keep their own tiers.
- Review runs one round. A second failure goes to the user.
- Speculative is expensive. Recommend it only for a real design fork, and keep the winner's worktree changes while discarding the rest.
