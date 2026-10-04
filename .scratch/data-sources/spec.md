# Data sources for expression variables

**Status:** ready-for-agent

Authority: [ADR-0006](../../docs/adr/0006-data-sources-for-expression-variables.md). Usage documentation: [docs/data-sources.md](../../docs/data-sources.md).

## Problem Statement

Term arguments are rule-text literals. A value that changes per request, or that lives in a JSON or YAML document, needs a bespoke predicate per field.

## Solution

A term argument may be a variable reference, `from("source", "query")`, resolved on each evaluation from a named `IDataSource` supplied with the evaluation. Sources own their query dialect, and a declared source name may carry an `IQueryValidator` so malformed queries are compile diagnostics; JSON and YAML use JSONPath (RFC 9535). Failures become `Unknown` plus a `Fault`.

## Out of scope

- Iteration over repeated subtrees and relative (`@.`) queries in rule text.
- A predicate form of a variable (`Json("$.flag")`).

## Tickets

See `issues/`. Numbered order is the build order.
