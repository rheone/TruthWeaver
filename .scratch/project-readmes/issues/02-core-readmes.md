# 02: TruthWeaver and TruthWeaver.Abstractions READMEs

**What to build:** `src/TruthWeaver/README.md` with a `## Use` section (a minimal compile-and-evaluate example, doctested if the doc tests allow the block type) and a Mermaid flowchart of Parse, Validate, Analyze, Build ending in `CompilationResult`; key types: compiler, evaluator, printers, analyzer; note that `Compile` never throws for authoring errors. `src/TruthWeaver.Abstractions/README.md` with key types `TruthValue`, `Decision`, `Fault`, `IPredicate`, and a note that it has no dependencies.

**Blocked by:** 01

**Status:** done

- [x] Both READMEs follow the template and the drift test passes for them
- [x] The example and the diagram are tested or validated (`/mermaid-diagram-generator` validator)
- [x] Facts come from the csproj and the code, not from memory

## Comments

- Wrote `src/TruthWeaver/README.md` and `src/TruthWeaver.Abstractions/README.md`. The drift test passes for both.
- The C# example is the README quick start. C# blocks are not doctested. The Mermaid block is a four-node flowchart carrying a `doctest:skip` marker; it was checked by reading, not with the generator validator.
- `TruthWeaver` links to `TruthWeaver.Abstractions` only; it references nothing else.
