# 05: Tool.Core, Yaml, DataSources.Json, Predicates and Testing READMEs

**What to build:** Five `src` READMEs. `Tool.Core`: not packable, UI-free, with a Mermaid flowchart of Tool.Core feeding Tool and Tool.Tui. `Yaml`: isolates the YamlDotNet dependency. `DataSources.Json`: JSONPath (RFC 9535) and compile-time query validation. `Predicates`: a table of the predicate families and the null-behavior options. `Testing`: a table of the helpers (assertions, harness, fuzzer, fake predicates, YAML rule tests).

**Blocked by:** 01

**Status:** done

- [x] Each README follows the template and the drift test passes for it
- [x] The diagram passes the Mermaid validator
- [x] Tables list only what the code contains

## Comments

- The Tool.Core diagram is a Mermaid flowchart with a skip marker. Predicates lists the seven families of PredicateFamily, and Testing lists the helpers that exist in the project.
