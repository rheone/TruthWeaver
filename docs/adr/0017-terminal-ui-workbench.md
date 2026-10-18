# ADR-0017: Terminal UI workbench

## Status

Accepted (2026-10-17). Decided with the owner when tickets 01 to 08 of the tui-workbench effort were built. The owner approved
Terminal.Gui as a dependency of the terminal UI project only. It adds one tool project to the set in
[ADR-0004](0004-package-boundaries-and-extensibility.md).

## Context

The command line (`TruthWeaver.Tool`) covers batch work. Authors also need an interactive place to write a rule, set the
value of each term, read the decision and the trace, and edit the tree. A UI library is a heavy dependency, and the core
and the tool core must not take it.

## Decision

1. **A separate tool project, `TruthWeaver.Tool.Tui`.** It packs as the .NET tool `truthweaver-tui` and a release does not
   publish it. It is the only project that references Terminal.Gui, and an architecture test checks that. Its tests are in
   `TruthWeaver.Tool.Tui.Tests`. The owner approved its reference to `TruthWeaver.Testing`, because the rule-test runner
   lives there; the command line references it too.
2. **The model has no view.** The state that the panes share (`RuleDocument`, `AssignmentSession`, the tree editor, the
   rewrite playground, import and export) lives in `TruthWeaver.Tool.Core`, so the CLI and tests use it without a
   terminal. The UI project holds only layout, key handling and drawing. A pane is one `IWorkbenchPane` and a key is one
   `WorkbenchCommand`, each registered with one appended line.
3. **An edit goes through the compiler.** A structural edit changes the JSON tree, compiles it and refuses an invalid
   result, so the editor never holds a rule that does not compile.
4. **Known limits.** Terminal.Gui 2.5 marks `TextView` obsolete in favor of a separate `Terminal.Gui.Editor` package. The
   project uses `TextView` with a scoped warning suppression until the owner approves that package. The palette does not
   offer the `TruthWeaver.Predicates` catalog, because that needs a new project reference.

## Considered options

- Put the model in `TruthWeaver.Tool.Tui`. Rejected: the CLI could not reuse it, and tests would need a terminal.
- Spectre.Console or a hand-written renderer. Not pursued: the owner approved Terminal.Gui.

## Consequences

Terminal.Gui brings transitive packages (Markdig, TextMateSharp with native libraries, and others). They are listed in
the [terminal UI page](../tui.md). Replacing the UI library later changes only the UI project.
