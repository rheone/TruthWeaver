# 04: Rule coverage over a corpus

**Group:** A understand a decision

**What to build:** Evaluate a rule over many recorded contexts (or accept many `Decision`s) and report which terms and branches ever decided the outcome, which never did, which were always `Unknown`, and which faulted. Output as a result type plus Markdown and JSON printers. Aim: find dead branches and flaky data in production samples. Build on the trace; do not re-evaluate if decisions are supplied.

**Blocked by:** 06 for the batch API shape (can start after 06's signature is fixed)

**Status:** ready-for-agent

- [ ] A coverage result lists per term: times evaluated, True/False/Unknown counts, fault counts, times it decided the outcome
- [ ] Branches that never decided are listed per operator
- [ ] Works from supplied decisions without re-evaluation
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
