# 21: Second-key tests prove the command ran

**What to build:** The tests catch a second key that never reaches its command. The test that presses every second key focuses the editor, presses a copy of the key and checks the effect of the command, so a view that swallows the key fails it. The clash test also compares each command key and second key with the Alt bindings of the editor, a list and a text field. Where a key cannot be tested through the key-event path, the ticket says why.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The every-second-key test runs with the editor focused and checks that the command ran (a status message, a changed state or an opened box), not only that the key event returned true.
- [ ] Alt+W, Alt+Q, Alt+Z and Alt+C have behavior tests (pane switch or divider move).
- [ ] The clash test fails when an Alt+letter second key is bound by the editor, a list or a text field.
- [ ] A test fails if a second key is swallowed by the focused view (show it by a temporary clash during development).
- [ ] Anything that cannot be tested without a terminal driver is written in the test summary.
