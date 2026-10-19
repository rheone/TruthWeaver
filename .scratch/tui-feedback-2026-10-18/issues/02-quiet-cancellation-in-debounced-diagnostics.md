# 02: Quiet cancellation in debounced diagnostics

**What to build:** Typing in the editor no longer raises a first-chance `TaskCanceledException` ('A task was canceled') for each superseded compile. The debugger output stays free of that exception during normal use.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] A superseded debounce or compile ends without an exception that a debugger reports.
- [x] Cancelling a pending compile when the user types again still works (existing tests stay green, one test added for the new path).
- [x] The timing of the diagnostics does not change.
