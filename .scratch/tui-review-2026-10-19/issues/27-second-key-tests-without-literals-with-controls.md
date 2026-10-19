# 27: Second-key tests without literals, with controls

**What to build:** The second-key tests read every key from the commands and prove what they claim. `SecondKeyTests` has no `Key.G.WithAlt`-style literal; it reads the key from the command it tests. The swallow demonstration has a positive control: the same key, with no binding added to the view, runs the command. The test of the clash helper covers the editor, a list and a text field. The keys that the window owns on purpose (Ctrl+Z and Ctrl+Y) are listed in one place with the reason.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] No test in `SecondKeyTests` hard-codes a command key or second key.
- [ ] The swallow test has a control that runs the command with the same key and no extra binding.
- [ ] The clash helper is tested against a binding in the editor, in a list and in a text field.
- [ ] The window-owned keys have one named list with a summary that gives the reason.
- [ ] All tests pass.
