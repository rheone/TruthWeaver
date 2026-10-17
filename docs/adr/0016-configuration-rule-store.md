# ADR-0016: Configuration rule store

## Status

Accepted (2026-10-17). Decided with the owner in the grilling of feature backlog ticket 12. The names and the rule format
were left open at that point and are chosen here for owner review. It adds one package to the set in
[ADR-0004](0004-package-boundaries-and-extensibility.md).

## Context

[ADR-0002](0002-evaluation-semantics.md) keeps rule storage and swap scheduling out of the library: a host compiles a
rule and swaps its own reference. Hosts that keep rules in application configuration repeat the same code: read the
text, compile, keep the last good rule, swap on change. The core must take no heavy dependency for this.

## Decision

1. **A new package, `TruthWeaver.Extensions.Configuration`.** The owner confirmed the package and its dependency on
   `Microsoft.Extensions.Configuration`. The core and the abstractions gain no dependency, and no other package
   references the new one. An architecture test checks both.
2. **Naming.** The project is `TruthWeaver.Extensions.Configuration`, the test project is
   `TruthWeaver.Extensions.Configuration.Tests`, and the namespace is `TruthWeaver.Extensions.Configuration`. The name
   follows the `Microsoft.Extensions.*` convention for a package that adapts a library to that stack.
3. **The source is an `IConfiguration` section.** Each child key is a rule name and each value is the rule. Reload
   follows the change token of the section. Directory watching and database or remote sources are later work.
4. **The rule format is rule text.** It is the smallest first format and needs no extra reader. JSON and YAML are not
   read, because a JSON or YAML rule is a whole document and does not fit a configuration value. A later change can
   add a format option.
5. **Caching and swap.** The store keeps one compiled rule per name, keyed by the SHA-256 hash of the rule text. A
   reload compiles only changed text and publishes the whole new set with one reference write.
6. **A bad reload keeps the last good rule.** The diagnostics go to the logger (an error) and to a reload callback
   (`Action<IReadOnlyList<RuleReloadReport>>`). A first load that fails leaves the name missing.
7. **A missing rule fails closed.** `EvaluateAsync(name, ...)` returns `Unknown` with no faults and logs a warning.
   `TryGet` is the non-failing lookup. The store has no `Get` that throws, because a missing rule is a normal state and
   the project does not use exceptions for control flow.
8. **Dependency scope.** The package references `Microsoft.Extensions.Configuration` only. It does not use
   `IOptionsMonitor`, so it does not reference `Microsoft.Extensions.Options`: the store needs the change token of a
   section, not bound options. The version is pinned to the .NET 11 RC line with the other `Microsoft.Extensions.*`
   packages, and the Dependabot ignore entry for that pattern covers it.
9. **Registration.** `AddTruthWeaverRuleStore<TContext>(sectionPath, onReload)` registers a singleton store that
   uses the container's `IConfiguration` and `RuleCompiler<TContext>`.

## Consequences

- The release set grows from nine to ten packages. The pack check, the release runbook and the package page list the
  new package.
- A host that needs another source or format writes a different store over the same compiler; nothing in the core
  changes.
