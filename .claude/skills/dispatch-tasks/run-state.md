# Run state

A long run outlives its context: the thread may be summarized, or the session restarted. The state file is the run's memory.

Keep it in the session scratchpad directory, named `dispatch-<slug>.md`. When the session names no scratchpad, use a temp location outside the repo. It is working state; never commit it.

## Contents

```text
Run: <slug>   Shape: chain | batch | graph   Mode: economy | fast   Cap: <n>
Heartbeat job: <id or none>
Bundles:
  B1  tier=<t>  model=<m>  after=<ids>  worktree=<path or none>  state=queued|running|done|failed
Tasks:
  1  B1  <title>  state=todo|done|failed|skipped|aborted  outcome=<one line>
Usage: B1 tokens=<n> duration=<s>
```

Rewrite the affected lines at every real outcome, in the same moment the checklist updates.

## Resume

When invoked to continue a run, or when the thread no longer shows the plan, read the state file first and rebuild the checklist from it.

- Bundles marked done or failed stay as recorded.
- A bundle marked running may have finished while the session was away, and its notification may be lost. List such bundles and ask the user whether to re-dispatch them.
- Re-check the heartbeat job id; call `CronCreate` again only when the job is gone.
- Continue at step 5 with the bundles that are ready.

Delete the state file at close-out once the summary is posted.
