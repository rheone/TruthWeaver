# 15: Generate families match the menu

**What to build:** The tick boxes of the Generate rule dialog mean what the Insert menu groups mean. Ticking Other can produce `COALESCE`, because the Insert menu lists it there. A group title the generator does not know fails loudly (a test fails) instead of silently becoming `If`. The real tick-box dialog keeps the dialog open when nothing is ticked.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Ticking only Other can produce `If` and `COALESCE` in generated rules, and a test checks it.
- [x] A test fails when an Insert menu group has no generator family.
- [x] The real tick-box dialog (not the scripted one) is tested: with nothing ticked, OK does not end the dialog.
- [x] `docs/tui.md` describes the groups as the menu does.
