# 26: Changelog and docs agree

**What to build:** The changelog and `docs/tui.md` tell one story. Two export entries contradict each other today: one says an image entry whose program is missing shows what to install, the other says image formats are listed only when the renderer is found. One entry still talks about a Generate group title that the generator does not know, but the check now uses a group identifier. Some entries carry a "(tui-feedback NN)" tag and some do not.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The older Export parity entry matches the current behavior (image formats are listed only when the renderer is found, and one note line names what to install).
- [ ] The Generate entry describes the check for a group with no generator family in the words of the current code, or leaves that detail out because users do not see it.
- [ ] The "(tui-feedback NN)" tags are consistent in the Unreleased list (all kept or all dropped).
- [ ] `docs/tui.md` says that OK with no box ticked keeps the Generate rule dialog open.
- [ ] The documentation tests pass.
