# 19: First-run hint for an empty rule

**What to build:** An empty rule shows a short hint instead of the raw message `TRE0001: Expected a term, constant, or '(' but found ''`. The hint says to type a rule, or to choose a sample or generate a rule from the menu, with the shortcut keys.

**Blocked by:** 15

**Status:** ready-for-agent

- [ ] An empty or whitespace-only rule shows the hint and no `TRE0001` error in the diagnostics pane.
- [ ] A rule with text still shows its real diagnostics.
- [ ] The hint names the real shortcut keys (a test compares them with the key bindings).
- [ ] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
