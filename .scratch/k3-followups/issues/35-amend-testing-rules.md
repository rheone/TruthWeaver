# 35: Amend the testing rules to describe how the repo tests

**What to build:** CLAUDE.md and AGENTS.md describe the real testing convention: Arrange/Act/Assert applies by shape, not by comment; `FakePredicates` is the default test double (a deliberate package) with NSubstitute for other seams; and the XML-summary rule applies to new and touched tests only. This resolves k3-followups 28 and k3-hardening 14. The 19 tests the branch added still get their summaries through k3-hardening 13, which stays as is.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] CLAUDE.md Testing and Development rules and AGENTS.md state the amended convention and agree with each other
- [x] k3-followups 28 and k3-hardening 14 are marked resolved by this ticket, with the decision recorded in the issues log
- [x] The k3-hardening 13 ticket text still matches the amended scope

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
