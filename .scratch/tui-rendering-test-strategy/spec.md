# TUI rendering test strategy

**Status:** needs-triage. Scoped as a decision, not yet grilled or broken into tickets.

Source: split out of `coverage-gaps-2026-10-25` ticket 19 during the 2026-10-25/26 triage pass, per the owner's decision to scope this as its own effort rather than fold it into that pass's per-boundary test tickets.

## Problem

`TruthWeaver.Tool.Tui`'s pure-presentation layer (`Hosting`, `Views`, `Panes`, `Workbench`, `Theme`) is near-0% covered across the merged coverage run: roughly thirty-plus classes, from whole-class gaps (`Hosting.TerminalDialogs`, `Program`, `Hosting.TerminalPrompts`, `Theme.TerminalBackground`) down to small 1-10 line gaps scattered across view/pane/workbench classes. Unlike the controller layer (`coverage-gaps-2026-10-25` ticket 18, already triaged) or the document/conversion model layer (ticket 17), this code is Terminal.Gui-based rendering — the "write a boundary-condition unit test" pattern that fits the rest of that effort doesn't apply here, because there's no established way to assert on rendered terminal output at all yet.

## What this effort needs to decide

- Whether to adopt golden-file rendering snapshots, a headless Terminal.Gui test driver, or some other approach — or a combination (e.g. a headless driver for interaction-heavy panes, snapshots for mostly-static chrome).
- How much of the ~30-class surface is actually worth covering this way versus accepted as exploratory-tested-only (some view/pane classes may be thin enough wrappers that a rendering test adds little).
- Whether any narrower slice of this layer turns out to contain real (non-rendering) logic worth testing directly with an ordinary unit test — if found, split that out as its own ticket the way tickets 17 and 18 did, rather than folding it into whatever strategy this effort picks.

## Not yet done

This file only records the problem and the decision to scope it separately. It has not been grilled into a design, and no tickets exist under `issues/` yet. Run `/grilling` (or `/grill-with-docs`) on this file before dispatching any work.
