# 23: Changelog and docs gaps

**What to build:** The changelog and docs record what the TUI fixes changed for users. Changelog entries cover the visible behavior of tickets 01 to 20: list dialogs end only on a real row and keep the highlight while filtering, the export dialog (Enter flow, term limit rule, image note, folder export), Generate rule ticks, the status line value, the moved Save into the document key and the second keys, and the inspect box. The `Coalescing` entry is shortened and has no project-history wording. The long Layout sentence in `docs/tui.md` is split.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The changelog has an entry for each user-visible change above, in the style of the other entries, with a link to the page that covers it.
- [ ] The `Coalescing` entry is about 25 words per sentence and names no ticket or review.
- [ ] No sentence in the Layout bullet of `docs/tui.md` is longer than about 25 words.
- [ ] The documentation tests pass.
