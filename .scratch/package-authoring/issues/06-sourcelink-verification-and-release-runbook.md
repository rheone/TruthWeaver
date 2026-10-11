# 06: Verify SourceLink and write the release runbook

**What to build:** SourceLink is configured (`Microsoft.SourceLink.GitHub`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, snupkg, deterministic CI builds). Verify it end to end on a local pack with `ContinuousIntegrationBuild=true`, using the `sourcelink` tool or `dotnet-validate` on each package and symbol package. Write `docs/releasing.md`: the steps to release (changelog, tag, pack, push, symbols), each marked as not run, and what to check afterwards.

**Blocked by:** 03, 05

**Status:** ready-for-agent

- [ ] Each package and its symbol package pass the SourceLink check, with the result recorded in the ticket
- [ ] `docs/releasing.md` exists, is linked from `docs/contributing.md`, and states that the runbook has not been run
- [ ] Nothing is pushed or released
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
