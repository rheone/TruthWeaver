# In-flight work

The k3-conformance effort (Strong K3 language surface, `.scratch/k3-conformance/`, tickets 01-31) is complete: [ADR-0005](../docs/adr/0005-strong-k3-language-surface.md) is the authority for the operator set, notation, boundaries, rewrites and diagnostics, and supersedes the operator-set, alias and `XOR`/`XNOR` decisions in ADR-0003 (marked in place). Open owner questions are summarised at the top of `.scratch/k3-conformance/issues-log.md`. The predicate catalog gap list (`.scratch/predicate-catalog/k3-gap-list.md`) is closed: every inventory predicate is implemented. Do not implement from `.scratch/k3-conformance/_superseded/` or `.scratch/engine-v1`. Material in `.tmp/` is reference only and may be subtly wrong; verify it.

A restructured multi-branch or multi-bundle plan (a new split of a ticket backlog across branches or bundles) is written to a durable `.scratch/<effort>/handoff-<date>.md` before any agent is dispatched against it. A plan that exists only in chat, an unsaved buffer or session scratch does not survive a context clear or a new session.

