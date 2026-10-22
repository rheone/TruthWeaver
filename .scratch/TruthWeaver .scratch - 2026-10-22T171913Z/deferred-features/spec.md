# Deferred features

Moved out of [CONTEXT.md](../../CONTEXT.md) so the public docs describe only
what exists today. Recorded here so these are revisited deliberately rather
than rediscovered from scratch. None of these are rejected outright — the
AST and compiler are designed so each remains addable without a breaking
rework.

| Item | Why deferred |
| --- | --- |
| **Authorization layer** (policy sets, permit/forbid, forbid-overrides, decision-with-provenance) | A genuinely different, larger problem than "evaluate one boolean expression." Belongs as a layer built *on* this engine, likely a separate package, once there's a concrete consumer. |
| **Partial evaluation / residual expressions** (bind known facts, simplify, hand the caller a residual expression to push into e.g. a SQL `WHERE` clause) | This is what "who can do X against many resources" really wants, but it requires predicates to be *translatable*, not just callable, which contradicts "a predicate is opaque application code." The engine is evaluate-only; callers loop over candidates, made cheap by per-evaluation memoization and a shared `CompiledRule`. |
| **Rule-to-rule references / named reusable fragments** | Valuable for a real rule library (shared sub-rules, cycle detection, compile-time inlining) but adds a resolver abstraction the current scope doesn't need. |
| **Cross-evaluation caching** | The predicate-author contract only promises stability *within* one evaluation. A cache spanning evaluations is a distinct feature with its own invalidation story. |
| **OpenTelemetry-shaped observability** (activity per rule, event per term, fault attributes) | Logging goes through `Microsoft.Extensions.Logging.Abstractions` today. OTel would be additive on top, not part of the current design. |
| **Context-bound term arguments** (e.g. `IsManagerOf({{resource.ownerId}})`) | Requires a typed path-expression mini-language and breaks static canonical-equality between rules. Arguments are literals only; a predicate that needs a live-resolved value — keyed by a rule-text literal, a `TContext`-supplied value, or both, with no requirement that either side be an identity or "the current user" — resolves it itself. See [README's "n arguments, class-based, externally-resolved value"](../../README.md#n-arguments-class-based-externally-resolved-value) for the documented alternative. See also [`.scratch/context-bound-term-arguments`](../context-bound-term-arguments) for prior investigation. **Superseded by [ADR-0006](../../docs/adr/0006-data-sources-for-expression-variables.md):** arguments may be `from("source", "query")` references to caller-supplied data sources. |
| ~~**Symbol operator aliases** (`&&`, `||`)~~ | **No longer deferred** — accepted on input, canonical form stays word-only; see ADR-0005 and `.scratch/k3-conformance/`. |
| **Concurrent operand evaluation** | Purely additive once predicates are contractually pure; left as an `EvaluationOptions` knob for later rather than the current default behavior. |
| **Minimal satisfying assignments** (BDD-derived "what facts would make this true") | The BDD exists anyway for constant/contradiction diagnostics; exposing satisfying-assignment enumeration is an authoring-tool feature with no current consumer. |
| **Attribute-based / assembly-scanned predicate registration** | Explicit registration only — scanning is magic, breaks trimming/AOT, and the repo's own rule is "do not introduce unnecessary abstractions." |
| **JSONC / JSON5 rule text** (comments and trailing commas in JSON rule text, or full JSON5 syntax) | Requested, but `System.Text.Json` does not cover this out of the box: `JsonDocumentOptions` (`CommentHandling`, `AllowTrailingCommas`) gets partway to JSONC, but nothing in the BCL handles JSON5's unquoted keys, single-quoted strings, or extended numeric literals. No parser for either exists in this repo yet. Needs a decision (hand-rolled subset parser vs. full JSON5 spec vs. a to-be-named external package) before implementation — deferred until that's investigated. |

## Re-score (2026-10-03, k3-hardening ticket 01)

Re-checked against the source after the Strong K3 work. The table above is the original reasoning; this table is the
current verdict for each item. "Still deferred" means the original reason still holds.

| Item | Current verdict | What changed |
| --- | --- | --- |
| Authorization layer | Still deferred | Nothing in `src/` is permit/forbid shaped. The building blocks it would sit on are now stronger: `RuleEquivalence.Compare` (policy comparison) and structured diagnostics. Still wants a concrete consumer. |
| Partial evaluation / residual expressions | Still deferred; in-process variant newly feasible | A residual for SQL still needs translatable predicates. But binding known predicates to constants and calling `CompiledRule.Simplify()` (K3-sound constant folding) would give an in-process residual; there is no substitution API yet. Scored in the [roadmap](../library-roadmap/spec.md#new-candidates). |
| Rule-to-rule references / named reusable fragments | Still deferred | Unchanged. The shared immutable `Expression` tree and `RuleBuilder` would make compile-time inlining straightforward, but the resolver abstraction and cycle detection are still unneeded. Related: "Joining compiled rules" in the roadmap. |
| Cross-evaluation caching | Still deferred | Unchanged. |
| OpenTelemetry-shaped observability | Partly delivered; tracing still deferred | `System.Diagnostics.Metrics` instrumentation exists (`TruthWeaverMetrics`, meter `TruthWeaver`: evaluations, faults, compile diagnostics), so a host can already `AddMeter("TruthWeaver")`. There is no `ActivitySource`, so per-rule activities and per-term events remain deferred. |
| Context-bound term arguments | Superseded | Replaced by data-source variable references ([ADR-0006](../../docs/adr/0006-data-sources-for-expression-variables.md)). `ResolvedValuePredicates` remains for a resolving client captured at registration. |
| ~~Symbol operator aliases~~ | No longer deferred | Already struck through. Unicode input aliases were also added (k3-followups 16). |
| Concurrent operand evaluation | Still deferred | Unchanged. The K3 operators added since (for example `COALESCE`, `If` and the cardinality family) do not change the evaluation contract; an `EvaluationOptions` knob remains the intended shape. |
| Minimal satisfying assignments | Partly delivered | `BddManager.FindSatisfyingAssignment` exists and powers the counter-example in `RuleEquivalence.Compare`, but it is internal and answers "where do two rules differ", not "what facts make this rule True". A public API is still an authoring-tool feature with no current consumer. |
| Attribute-based / assembly-scanned predicate registration | Still deferred (runtime scanning rejected) | Unchanged. The compile-time source-generator variant is scored separately in the [roadmap](../library-roadmap/spec.md). |
| JSONC / JSON5 rule text | Still deferred | Unchanged. JSON diagnostics now carry source spans (commit 2f296ff), which any future lenient parser would need to keep. |

New deferred items from the K3 work (diagnostic properties and JSON pointer, k3-followups 26; lint spans, k3-hardening 09)
are tracked in the [roadmap's new candidates](../library-roadmap/spec.md#new-candidates) rather than duplicated here.

## Added 2026-10-09 (grilling session)

| Item | Why deferred |
| --- | --- |
| **Structured, serializable render tree for UI consumers** (a public, JSON-friendly form of `RuleRenderTree`/`RenderNode`/`RenderState`) | From `diagram-rendering-options`. A rule-authoring web UI would draw its own diagram instead of parsing Mermaid text. No UI consumer exists yet. |
| **Mermaid web-UI knobs** (`click` tooltips and callbacks, HTML/CSS labels, custom themes under a relaxed `securityLevel`) | GitHub's `strict` security level strips them, so they only matter to a host that embeds mermaid.js. Same reason as above. |
| **Public satisfying assignment and substitution (`Bind` plus `Simplify()`)** | Confirmed deferred on 2026-10-09. K3 assignments need a designed result type and there is no consumer. |
| **Tracing (`ActivitySource`)** | Confirmed deferred on 2026-10-09. Metrics and evaluated-tree traces cover current needs. |
| **Negation-normal-form print mode, lint spans and paths** | Confirmed "Not now" on 2026-10-09. `Simplify()` covers most of NNF. Spans need every expression node to carry a source location. |
