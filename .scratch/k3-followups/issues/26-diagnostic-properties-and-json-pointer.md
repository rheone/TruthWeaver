# 26: Typed `Diagnostic.Properties` and `ToJsonPointer()` (deferred)

**What to build:** Two diagnostic additions the research recommends only when a consumer needs them. First, an optional string `Properties` map on `Diagnostic`, so a UI can branch on machine-readable data without parsing the free-text `Expected` and `Found` phrases (Roslyn's `Properties` and SARIF's `properties` bag are the precedent). Second, a `ToJsonPointer()` on `Diagnostic.Path` that converts the RFC 9535 singular-query form to an RFC 6901 JSON Pointer for JSON Schema and validator interop. Neither is built until a consumer asks. When one does, the consumer's need decides the exact shape, so re-read the research findings (section 5) and confirm the scope with the owner first.

**Blocked by:** a consumer that needs it

**Status:** deferred

- [ ] A consumer need is identified and the owner confirms the scope
- [ ] `Diagnostic.Properties` (if wanted) is an optional string map, with `Expected` and `Found` unchanged
- [ ] `ToJsonPointer()` (if wanted) escapes `~` and `/` per RFC 6901 and round-trips for every path the compiler produces
- [ ] Documentation updated in the same change: `docs/diagnostics.md` documents `Properties` and `ToJsonPointer()` (if built)
- [ ] The full validation from CLAUDE.md passes

Decision (rule-linting 01): the link from a nested lint finding to its enclosing finding is a dedicated `Diagnostic.EnclosedBy` property, not an entry in `Properties`. `Properties` stays a string map for machine-readable data and does not conflict with it.

Source: [research findings, section 5](../../k3-conformance/research-findings.md#5-api-shape-and-naming).
