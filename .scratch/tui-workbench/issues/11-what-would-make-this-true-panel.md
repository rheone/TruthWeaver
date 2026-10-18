# 11: What would make this True panel

**What to build:** A pane that lists the smallest changes to the assigned term values that flip the decision to True (or to the value the author picks), using the counterfactual search of the library (`docs/counterfactuals.md`). Each entry shows the terms that change and their new values. Picking an entry applies it to the Assign pane. The pane shows when the search hits its work limit instead of hanging, and it runs off the UI thread.

**Blocked by:** 20

**Status:** done

- [x] The panel lists the same change sets as the library call for the same rule and values
- [x] Picking an entry sets the assigned values and the decision updates
- [x] A work-limit result shows a message and the editor stays responsive
- [x] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- The pane is "Make True", the second pane after Validation in the bottom region. `CounterfactualController` calls `RuleCounterfactual.FindChanges(rule, decision)` with the decision of the Assign pane, on `Task.Run`, and posts the result through `Workbench.Post` (from ticket 20). A version counter drops an older result.
- Choice: the target is True by default, and `t` cycles True, False, Unknown (the ticket says "or the value the author picks").
- Choice: no fixed terms. A change to a `from(...)`-reference term is refused at apply time with a status message, because the assignment keys that term by its resolved value.
- Choice: the library gives no reason for `IsComplete == false`, so the pane tells the two limits apart by the count: 10 sets means the cap, fewer means the 100,000-step work limit. A rule over the term cap (`Undecided`) shows the library reason.
- Tests: `CounterfactualPaneTests`. The work-limit message of the 100,000-step case is not run in a test (it needs a rule that takes that many steps); the cap message uses the same branch.
