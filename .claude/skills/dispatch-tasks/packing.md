# Packing tasks into bundles

A bundle's cost is a fixed cold start, plus the work of each task, plus context drag: every later turn carries everything the agent has read so far. Too few tasks per bundle wastes cold starts. Too many drags and degrades the agent. Bin-pack by weight to land between.

## Weigh each task

**Tier** (what the work demands):

- _mechanical_: the answer is already specified. A rename, a formatting fix, a stated edit at known places, XML docs for a named member.
- _standard_: bounded work in one subsystem that follows existing patterns. Implement a small feature, write tests for known behavior, investigate a narrow question.
- _judgment_: design choices, an ambiguous spec, behavior across packages, an unknown cause. Anything where a wrong call is expensive to find later.

**Size**: small (2 files or fewer), medium (3 to 6), large (7 or more, or crossing a package boundary).

| Weight | Mechanical | Standard | Judgment |
| --- | --- | --- | --- |
| Small | 1 | 2 | 3 |
| Medium | 2 | 3 | 5 |
| Large | 4 | 6 | 8 |

When the signals disagree, take the heavier cell.

## Models

Each tier maps to a model: mechanical to the cheapest model the `Agent` tool offers, standard to the current session model, judgment to the strongest. Read the options from the tool in this session; do not assume a roster. A model or `subagent_type` the user named wins, for the whole run or for the bundles they name.

## Escalation

A failed task is retried once, as a new bundle on the next tier up (mechanical to standard, standard to judgment). The retry prompt carries the task, the failure's blocker, and the files the first attempt changed. Judgment has no tier above it, so its failures go to the user.

An interrupted bundle is not a failed one: a rate limit, a timeout, or another infrastructure error that cuts a bundle off mid-task leaves its work saved, not wrong. Resume it with `SendMessage` to the same agent once the interruption clears, picking up exactly where it stopped - never dispatch a fresh bundle for the same tasks, which redoes real work and spends tokens twice. Only a genuine wrong-output failure gets the retry-a-tier-up treatment above.

## Bin-pack

Starting values, not measured: **budget 8 points** and **at most 6 tasks** per bundle. Tune them from the usage that step 8 of SKILL.md records.

Measured so far (rule library fixes, 2026-11-01):

| Bundle | Model | Tasks | Tokens | Tool calls | Time |
| --- | --- | --- | --- | --- | --- |
| Three judgment tasks in one subsystem | strongest | 3 | about 242,000 | 164 | about 54 min |
| Two standard tasks in one class | session model | 2 | about 81,000 | 67 | about 34 min |
| Three judgment tasks across diagnostics and tracing | strongest | 3 | about 263,000 | 175 | about 42 min |

A bundle of three judgment tasks costs about 250,000 tokens. Each commit hook adds about 4 minutes, so a bundle that commits per task spends 8 to 12 minutes in hooks.

1. Group tasks by tier. A bundle holds one tier, so the model fits every task in it.
2. Within a tier, cluster by cohesion: same files, subsystem or package. Shared reads are where bundling saves tokens.
3. Fill bundles first-fit-decreasing: heaviest task first, each into the first bundle with room. A task of weight 6 or more runs solo.
4. Tasks with no after edge between them whose descriptions name overlapping files go into the same bundle, ordered as listed. If that overflows the budget, keep the oversized bundle and flag it in the plan. Count a test class as a file: two tasks that add tests to the same test class are in the same bundle, or in a chain, never in parallel, because both append at the same spot and the merge conflicts.
5. A bundle runs its tasks in dependency order and starts once every dependency lying outside the bundle is done. Along a linear stretch of the graph (a chain), merge adjacent same-tier tasks up to the budget. A tier change, a full budget, or a point where the graph fans out or in ends a bundle.
6. A reviewer, judge or reduce task never shares a bundle with the work it checks; independent eyes are the point.

## Modes

- **economy** (default): the packing above. Fewest agents and least cold-start cost, with less parallelism.
- **fast**: one task per bundle, each still on its tier's model, run up to the concurrency cap with the rest queued. Most parallel, highest cost.

The plan always names the other mode with its bundle count, so switching takes one word.

## Concurrency

Default cap: 3 bundles at once. The cap counts bundles, not tasks, so economy rarely reaches it. State the cap in the plan; a higher number is the user's call there.
