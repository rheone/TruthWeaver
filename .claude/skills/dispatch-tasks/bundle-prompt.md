# Bundle prompt and report

Each bundle's prompt is the whole brief; the sub-agent has no other context. Build it from this template.

```text
You are carrying <n> task(s). Do them in the listed order, finishing each before the next.

Context every task shares:
<facts, paths and constraints stated once>

Tasks:
1. <task text>
   Done when: <criterion>
2. ...

Verify each task with the narrowest check that covers your change (the project documents its targeted test and build commands). The full validation runs once after integration.
A bundle that adds a public type or a diagnostic code also runs the full `dotnet test` once before it reports (about a minute), because a catalog or registry in another package can fail on it.
If a task fails, continue with the later tasks that do not depend on it and mark the dependent ones skipped.
Before you report, when another bundle is running beside you against the same target branch: check your diff against that branch's current state (a dry-run merge, if your tools support one) and name any likely conflict area in your Handoff, even without seeing the sibling's actual diff - a clean exit from your own worktree is not evidence the integration will be clean too.

Reply in exactly this shape:
Task 1: done | failed | skipped - <one-line outcome>
  files: <paths changed>
  checked: <what you ran and its result>
  blocker: <only when failed or skipped>
Handoff: <facts a later bundle needs, 5 lines at most; a likely conflict area from the check above; omit when none>
```

Chain bundles after the first also get the prior report's `Handoff` lines under "Context every task shares". Pass only those lines; the prior full report stays in this thread.

Add `Do not commit.` for any bundle that writes to the repo, and `Do not modify files.` for read-only bundles (map, review, judge).

When the bundle runs in a worktree and the work stacks on a branch other than `main`, put this first in the prompt, because a worktree starts on `main`: "Before anything else, confirm `git rev-parse --show-toplevel` is a path under `.claude/worktrees/`, then run `git merge --ff-only <tip>`. Stop and report when either fails."

A retry bundle adds a "Prior attempt" block under the shared context: the task, the blocker, and the files the first attempt changed. A review bundle gets the work's done-when and the diff to check.

The fixed reply shape keeps results cheap to relay and lets step 6 of SKILL.md check each `Done when` against a stated outcome.
