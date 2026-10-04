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
If a task fails, continue with the later tasks that do not depend on it and mark the dependent ones skipped.

Reply in exactly this shape:
Task 1: done | failed | skipped - <one-line outcome>
  files: <paths changed>
  checked: <what you ran and its result>
  blocker: <only when failed or skipped>
Handoff: <facts a later bundle needs, 5 lines at most; omit when none>
```

Chain bundles after the first also get the prior report's `Handoff` lines under "Context every task shares". Pass only those lines; the prior full report stays in this thread.

Add `Do not commit.` for any bundle that writes to the repo, and `Do not modify files.` for read-only bundles (map, review, judge).

A retry bundle adds a "Prior attempt" block under the shared context: the task, the blocker, and the files the first attempt changed. A review bundle gets the work's done-when and the diff to check.

The fixed reply shape keeps results cheap to relay and lets step 6 of SKILL.md check each `Done when` against a stated outcome.
