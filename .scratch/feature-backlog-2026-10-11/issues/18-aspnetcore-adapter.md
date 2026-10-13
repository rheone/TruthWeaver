# 18: ASP.NET Core authorization adapter

**Group:** D tooling and integration

**What to build:** A new package `TruthWeaver.AspNetCore` (framework reference `Microsoft.AspNetCore.App`, no other NuGet dependency) with test project `TruthWeaver.AspNetCore.Tests`. Scope decided 2026-10-11: (1) an `IAuthorizationHandler` and a policy builder (`AddTruthWeaverPolicy("name", rule)` / `RequireRule`) so `[Authorize(Policy = ...)]`, `IAuthorizationService.AuthorizeAsync(user, resource, policy)` (resource-based authorization), and `AuthorizeView` work unchanged; (2) a shipped `AuthorizationContext<TResource>` carrying `ClaimsPrincipal User`, `TResource Resource` and `HttpContext? Http`, with ready-made predicates (`HasClaim`, `IsInRole`, `IsAuthenticated`, `IsOwner` by selector) AND a generic mapper so a host can supply its own context type; (3) fail closed: satisfied succeeds, `False` or `Unknown` fails (403, or 401 when unauthenticated), the `Decision` (with faults and trace) is kept on the failure for logging and a problem-details response, and a per-policy option can allow `Unknown`, off by default; (4) DI registration extensions following `TruthWeaver`'s existing DI style. Endpoint filter is a later ticket.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Before any file is created, the worker confirms the scope with the owner (standing rule: new packages, new projects and heavy dependencies need explicit confirmation); the confirmed scope is written at the top of this ticket
- [ ] A sample minimal-API app (under `samples/`) shows a policy, a resource-based check and the fail-closed behavior, with tests using `TestServer`
- [ ] `Unknown` and faults deny by default; the allow-Unknown option is tested off and on
- [ ] The decision is available to a logger and a problem-details writer
- [ ] Package metadata, README, pack check and docs page (`docs/aspnetcore.md`) are in place; trim and AOT impact is documented
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
