# Mermaid render options

**Status:** ready-for-agent

Source: [diagram-rendering-options](../diagram-rendering-options/spec.md), Mermaid-text track, grilled 2026-10-09. The
structured render tree for web UIs is a separate spec.

## Problem Statement

`MermaidTreePrinter` has two knobs (`OperatorStyle`, `showArgumentValues`) and `Decision` coloring. The diagram direction
is fixed at `flowchart TD`. Every node is the same rectangle. A term's label and its arguments sit in one dense line. A
caller cannot highlight or mute a node without an evaluation, and cannot change the colors. Large flat `AND` or `OR`
chains draw one edge per operand.

## Solution

A `MermaidOptions` record that carries the new knobs, used by `MermaidTreePrinter.Print` and `CompiledRule.PrintMermaid`.
The output stays valid on GitHub's sanitizing renderer by default.

## Decisions

- **Knobs.** Direction (`TD`, `LR`, `BT`, `RL`; default `TD`), node shape by role (operator, term, constant), two-line term
  labels, a per-node style callback, a palette, and chain compaction.
- **Options API.** `MermaidOptions` holds all behavior, including the existing `OperatorStyle` and `ShowArgumentValues`.
  The current overloads and a fluent builder are thin front-ends that build a `MermaidOptions` and call the one
  implementation. Three call styles, one code path, tested through the record.
- **Node shape.** A distinct shape per role, chosen from the existing node kind: operators, terms and constants each get
  one. A default set ships and is switchable off for the plain rectangle.
- **Two-line term labels.** A term renders as a bold predicate label and a plain second line with the arguments, using
  Mermaid markdown-string labels (backtick form). Raw HTML is not used, because GitHub strips it. Dimmed or smaller text
  is not promised, because only a self-hosted renderer can apply it. The documentation states this.
- **Style callback.** `Func<OutlineNode, NodeStyle?>` picks a style for any node: `Highlight`, `Mute` or a custom class
  name. It runs after `Decision` coloring and wins on conflict. It is not serializable, by choice.
- **Palette.** Presets `Light` (today's colors, the default), `ColorblindSafe`, `Monochrome` and `Dark`, plus a
  caller-supplied palette record. Each defines the true, false, unknown and skipped classes plus highlight and mute.
  `Monochrome` separates states by stroke style so it prints in grayscale.
- **Chain compaction.** A flat `AND` or `OR` of leaf terms is drawn inside a Mermaid `subgraph` box when its operand count
  exceeds a threshold. Nothing is hidden. Compaction is off by default.
- **Defaults are backward compatible.** With no options, the output equals today's output.

## Out of scope

- The structured, serializable render tree for web UIs.
- Click handlers and tooltips (they need a relaxed `securityLevel`).
- Full HTML or CSS labels.
- Other Mermaid diagram types, and an evaluation-trace sequence diagram.

## Further notes

- Tickets: direction and shapes; two-line labels; palette; style callback; chain compaction; fluent builder and
  overload front-ends last, over the finished record.
- Every ticket validates its Mermaid output with the repository's Mermaid test or `validate-mermaid.mjs`. The doc
  examples carry `doctest` markers per `docs/doc-examples.md`.
- No operation or catalog predicate is added, so the K3 reference sync does not apply.
