# Task sources

Take the first source that applies. Tickets and plans are optional: a project may have neither.

1. **The invocation lists the work.** Use the list as given; it is unambiguous and needs no confirmation of the list itself.
2. **The invocation names tickets** (a number, a range, a path, a feature directory, "all open") or says "tickets" without naming any. See Tickets below.
3. **The invocation points at a plan** (pasted, a file, or earlier in this conversation). See Plans below.
4. **Nothing is listed.** Derive a numbered list from the recent conversation. It is inferred, so the plan in step 4 shows it for approval.

## Tickets

1. Find the tracker convention. Look in `CLAUDE.md` / `AGENTS.md` and the docs for an issue-tracker document (for example `docs/agents/issue-tracker.md`) and follow it for file locations, status lines and blocker lines. Without one, treat each markdown or issue file the user names as a ticket. With no tracker and no named file, say so and fall back to a plan or the conversation.
2. Target what the user named. Without targeting, propose every open ticket in the named feature, and ask in the plan which feature when several exist.
3. Map each ticket to a task:

| Ticket part | Becomes |
| --- | --- |
| Title and "what to build" | the task text |
| Acceptance checklist | the done-when |
| Blocker line (`Blocked by`) | after edges |
| Status already done | skipped, noted in the plan |
| Status claimed by someone else | flagged in the plan |

A blocker outside the run that is not done is flagged in the plan, and the user chooses: add it, or leave out its dependents.

4. Status writeback is part of the plan. When approved, update each finished ticket in close-out, after validation, in the tracker's own format.

## Plans

A step or phase that has its own outcome is one task. Keep the plan's wording and scope. Plan order sets the edges ("then", "after", "phase 2"); steps the plan marks parallel stay unordered. The derived list is inferred, so step 4 shows it for approval.

## Self-contained tasks

A bundle prompt is the sub-agent's whole brief. Give each task the text it needs, and for anything longer than about 30 lines name the file and section so the agent reads it, instead of pasting it.
