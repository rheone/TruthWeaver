# 02: TruthWeaver and TruthWeaver.Abstractions READMEs

**What to build:** `src/TruthWeaver/README.md` with a `## Use` section (a minimal compile-and-evaluate example, doctested if the doc tests allow the block type) and a Mermaid flowchart of Parse, Validate, Analyze, Build ending in `CompilationResult`; key types: compiler, evaluator, printers, analyzer; note that `Compile` never throws for authoring errors. `src/TruthWeaver.Abstractions/README.md` with key types `TruthValue`, `Decision`, `Fault`, `IPredicate`, and a note that it has no dependencies.

**Blocked by:** 01

**Status:** ready

- [ ] Both READMEs follow the template and the drift test passes for them
- [ ] The example and the diagram are tested or validated (`/mermaid-diagram-generator` validator)
- [ ] Facts come from the csproj and the code, not from memory
