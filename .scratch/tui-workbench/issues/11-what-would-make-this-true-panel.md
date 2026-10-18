# 11: What would make this True panel

**What to build:** A pane that lists the smallest changes to the assigned term values that flip the decision to True (or to the value the author picks), using the counterfactual search of the library (`docs/counterfactuals.md`). Each entry shows the terms that change and their new values. Picking an entry applies it to the Assign pane. The pane shows when the search hits its work limit instead of hanging, and it runs off the UI thread.

**Blocked by:** 20

**Status:** ready (owner decision 2026-10-17)

- [ ] The panel lists the same change sets as the library call for the same rule and values
- [ ] Picking an entry sets the assigned values and the decision updates
- [ ] A work-limit result shows a message and the editor stays responsive
- [ ] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
