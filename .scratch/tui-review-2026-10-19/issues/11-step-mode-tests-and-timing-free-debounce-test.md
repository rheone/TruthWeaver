# 11: Step-mode tests and timing-free debounce test

**What to build:** The tests cover what the step mode promises, and the debounce test no longer depends on a real delay. Tests step a term that faults and check its message at the node, and step back with Previous. The debounce test uses a fake clock or timer.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] A test steps a faulting term and checks the fault message in the step and the status line.
- [x] A test uses Previous more than once and checks the marks.
- [x] The debounce test has no real `Task.Delay` and still checks the timing rule.
