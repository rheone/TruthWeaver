# 27: Time windows use an absent argument, not an empty string

**What to build:** The `end` and `duration` arguments of the time-window predicates use `""` for "not given", so `end: ""` is silently valid. Decide how to say "not given". Recommended: an absent argument, with the validator telling "omitted" from "from a data source" another way.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Decision (owner, 2026-10-10):** An omitted argument means "not given"; `""` becomes an error. The validator tells "omitted" from "from a data source" another way.

- [x] The owner picked the mechanism
- [ ] The window pages and tests are updated
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
