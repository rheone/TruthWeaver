# 27: Time windows use an absent argument, not an empty string

**What to build:** The `end` and `duration` arguments of the time-window predicates use `""` for "not given", so `end: ""` is silently valid. Decide how to say "not given". Recommended: an absent argument, with the validator telling "omitted" from "from a data source" another way.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the mechanism
- [ ] The window pages and tests are updated
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
