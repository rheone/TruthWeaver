---
name: dispatch-tasks
description: Dispatch work to sub-agents (serially, in parallel, or in dependency order) and relay each one's status into this thread as it lands. Use for "dispatch", "for each" over a list, "in parallel", "serially", working through tickets or a plan with agents, or handing work to a background "thread".
argument-hint: "What should be dispatched (a list, tickets, or a plan), and serially or in parallel?"
license: Apache-2.0
metadata:
  version: "2.0.0"
  tools: "Agent, ToolSearch, CronCreate, CronDelete"
  author: Robert H. Engelhardt <rheone@gmail.com>

---

Run work on sub-agents instead of inline, and keep this thread posted as each piece lands. A run is a graph of tasks with **after** edges, in one of three shapes: a **chain** (every task after the previous), a **batch** (no edges), or a **graph** (some tasks wait on others).

The unit of dispatch is a **bundle**: the tasks one sub-agent carries. Each sub-agent pays a cold start, so tiny bundles waste tokens, and overloaded bundles thin the agent's attention. Bin-pack tasks into bundles by weight and tier, propose the plan, and let the user approve or override it.

1. **Gather the tasks.** Read [sources.md](sources.md) and take the list from the invocation, the tickets or plan it points at, or the recent conversation. Give each task a **done-when** criterion.
   Done when: a numbered task list exists, every task has a done-when, and any inferred list is marked as inferred.

2. **Order the tasks.** Set the after edges from the source (ticket blockers, plan order, wording such as "serially", "in parallel", "for each", "then"). Name the resulting shape. When a request fits a shape in [recipes.md](recipes.md), recommend that recipe.
   Done when: every task has its after edges or none, and the shape is named.

3. **Weigh and bundle.** Read [packing.md](packing.md) and follow it: assign each task a tier and weight, then bin-pack into bundles. Default to **economy** (pack tight); keep **fast** (one task per bundle) ready as the alternate. Take any `subagent_type`, `model`, concurrency cap or heartbeat setting the user already named; otherwise use the defaults in packing.md and heartbeat.md.
   Done when: every task sits in exactly one bundle, each bundle has a tier and model, and the fast-mode bundle count is known.

4. **Propose and approve.** Post one message holding the whole plan: shape and recipe, a table of bundles (tasks, tier, model, after), concurrency, worktree isolation, integration, escalation (default: one retry a tier up), ticket status updates (when tickets are the source), the state file path, whether a heartbeat is suggested, the alternate pack mode with its bundle count, and anything flagged (oversized bundle, collision folded into one bundle, a blocker outside the run). Mark it as the recommendation and invite an override of any part. Proceed on a yes or a revised request; revise and repost on a revision. A one-task run skips this step.
   Done when: the user approved the plan or replaced it.

5. **Dispatch.** Build each bundle's prompt from [bundle-prompt.md](bundle-prompt.md). A bundle is **ready** when every dependency of its tasks that lies outside the bundle is done. Dispatch ready bundles up to the concurrency cap, all in one message.
    - A lone ready bundle (the usual case in a chain) runs with `run_in_background: false`, so its report arrives before the next prompt is built. Several ready bundles run with `run_in_background: true`; each completion notification dispatches whatever just became ready, and queued bundles take freed slots at once.
    - A bundle that writes and can run beside another writing bundle gets `isolation: "worktree"`. Other bundles run in this checkout. Feed a dependent bundle only the `Handoff` lines of its predecessors.
    - A failed task is retried once as a new bundle one tier up, with the failure in its prompt (see [packing.md](packing.md)). When the blocker needs a user decision, ask instead. A task still failing after that ends its dependent tail, and independent bundles keep going.
   Done when: every bundle is dispatched, queued, or aborted with a stated reason.

6. **Keep a live checklist and state file.** Post one checklist (☐/☑ per task, grouped by bundle) and update it in place at each real outcome, and mirror it into the state file per [run-state.md](run-state.md). Post each outcome as it lands.
   If the plan armed a heartbeat, run it as [heartbeat.md](heartbeat.md) describes.
   Done when: every row is checked with an outcome or marked aborted.

7. **Integrate.** Applies to bundles that wrote in a worktree. Bring each one's changes into this checkout in completion order, leaving them uncommitted, and before any bundle that depends on it starts. On a conflict, stop and show it to the user. Then run the project's required validation (from `CLAUDE.md` / `AGENTS.md`) once, in this checkout. Workers ran only the narrowest check that covers their change.
   Done when: every worktree's changes are in this checkout or reported as conflicted, and the validation result is reported.

8. **Close out.** Cancel the heartbeat if one was armed. When the plan included ticket status updates and validation passed, update each finished ticket per the tracker's convention. Then post one summary: each task with its outcome, aborted ones included, plus each bundle's reported token and duration usage when the result carries it, so [packing.md](packing.md) weights can be tuned.
   Done when: the heartbeat is deleted, and the summary accounts for every task from step 1.
