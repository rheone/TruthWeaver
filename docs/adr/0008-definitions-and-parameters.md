# ADR-0008: Definitions and parameters

## Status

Accepted (2026-10-16). Amends [ADR-0005](0005-strong-k3-language-surface.md)'s operator and notation list
in place (dated note below). Records the scope the owner set on
`.scratch/feature-backlog-2026-10-11/issues/11-named-sub-rules-and-parameters.md` (2026-10-11), ahead of
the grammar change described there.

## Context

A rule author who repeats the same sub-expression, such as an age check used by several branches of one
rule, today has to write that sub-expression out each time, in every form (DSL text, JSON, YAML and
`RuleBuilder`). The `RuleBuilder` form already lets a C# caller stash a `RuleBuilder` value in a variable
and reuse it, but the DSL, JSON and YAML forms have no equivalent, and nothing lets any of the four forms
parameterize a reused shape (an age check reused at two different thresholds, say).

Ticket 11 asks for one mechanism, used the same way from all four forms, that lets an author name a
sub-expression once and use it, optionally with arguments, as many times as needed inside one rule
document. Ticket 11-1, this ADR, records that mechanism and its guardrails before the grammar, schema and
builder changes (tickets 11-2 through 11-5) touch any code.

## Decision

1. **A definition is compile-time, document-scoped sugar, not a new tree node.** A rule document (one DSL
   source, one JSON or YAML document, or one `RuleBuilder` build) may declare zero or more named
   definitions. Each use of a definition's name is replaced, at compile time, by the definition's own
   expression tree with its parameters substituted. The compiled `Expression` tree that reaches the
   evaluator, the BDD analyzer, the rewrites and the printers never contains a definition or a use of one;
   it contains only the expanded primitive and derived operator nodes it already understands. No new
   `NodeShape`, no new evaluator case, no new analyzer rail and no new printer case are needed for
   definitions themselves.
2. **Rule text: `let`.** A rule text document is zero or more `let` statements followed by exactly one
   expression, the rule's value. `let name = expression;` declares a definition with no parameters;
   `let name(p1, p2, ...) = expression;` declares one with parameters, each usable inside `expression`
   wherever a term, constant or sub-expression could appear. A use is the name applied like a call
   (`atLeastAge(21)`) or, for a parameterless definition, written bare (`adult`). `let` is a reserved word,
   case-insensitive, alongside the existing reserved operator and constant words.
3. **JSON and YAML: a `definitions` section in the tree format.** A document may carry a `definitions`
   object (JSON) or mapping (YAML) alongside its existing root, keyed by definition name; each entry is
   `{"parameters": ["p1", "p2"], "body": <tree>}` (`parameters` omitted or empty for a parameterless
   definition). A use is a new node shape, `{"op": "use", "name": "atLeastAge", "operands": [<arg1>, ...]}`,
   wherever a tree node is expected; `operands` supplies the arguments positionally and is omitted or empty
   for a parameterless use. `rule-tree.schema.json` gains the `definitions` section and the `use` node.
4. **`RuleBuilder`: a definition API that expands the same way.** The builder already lets a caller hold a
   `RuleBuilder` value in a C# variable and reference it from several places in the tree being built; that
   remains the ordinary, zero-overhead way to reuse an un-parameterized shape and is not itself a
   definition. The builder's new, explicit definition API (named in ticket 11-4) additionally supports
   parameters, and compiles, through the same JSON tree the builder already renders
   (`RuleCompiler.CompileJson(JsonElement)`), to the `definitions` section and `use` nodes of decision 3 —
   so a builder-authored rule with definitions produces the identical compiled tree a DSL or JSON author
   would get for the same definitions and uses (the cross-form equality ticket 11-5's property test checks).
5. **Parameters take literals or sub-expressions; no loops, no recursion.** An argument at a use site is
   anything a rule-text operand, JSON/YAML tree node or `RuleBuilder` node can be: a literal, a term, or a
   nested sub-expression (including a use of another definition). Substitution is purely textual/structural
   at expansion: a parameter is replaced, in the definition's body, by the argument tree from the use site
   that supplies it. There is no loop construct and no way for a definition to call itself, directly or
   through another definition, at any depth (decision 6 makes self-reference a compile error rather than
   leaving it to a runtime bound). A definition with parameters is not a function in the host language
   sense: it has no local-only bindings beyond its declared parameters, and a parameter that shadows an
   enclosing name is out of scope because definitions do not nest inside expressions.
6. **Guardrails, each a distinct compile diagnostic:**
   - **Cycles.** A definition cannot reference itself, directly or through any chain of other definitions.
     The compiler detects the cycle during expansion (before it would recurse unboundedly) and reports an
     error naming every definition in the cycle, in the order it found them.
   - **Name collisions.** A definition name must be unique within its document; it cannot reuse a reserved
     word (including `let` itself), the name of a predicate registered with the compiler, or `T`/`F`/`U`.
     Each is a compile error, matching the existing reserved-word and predicate-name diagnostics in spirit
     (one code, reused rather than duplicated, with a message that says which kind of name was clashed
     with).
   - **Expanded size counts against the normal limits.** Once a use is expanded, its nodes are ordinary
     tree nodes: they count toward `CompilerOptions.MaxNodeCount` and `CompilerOptions.MaxDepth` exactly as
     if the author had written them out by hand, and the existing `TRE0009`/`TRE0010` diagnostics apply
     with no special-casing for where the nodes came from. A term a definition repeats keeps one shared
     identity after expansion when the arguments are identical, consistent with the engine's existing term
     identity rule — this does not relax `MaxNodeCount`, which counts the tree as printed, matching the
     existing rewrite-size accounting ADR-0005 decision 10 already established for `ExpandToPrimitives`.
   - **Definition nesting depth.** A new `CompilerOptions.MaxDefinitionDepth` bounds how many definitions
     deep a use may expand through (a use inside definition A's body that names definition B, whose body
     names C, and so on). Exceeding it is a distinct compile error, independent of `MaxDepth` (which bounds
     the expanded tree's structural depth, not the definition chain) and independent of the cycle check
     (which would otherwise require unbounded recursion to detect a non-cyclic but pathologically deep
     chain).
   - **Argument count and unknown definitions.** A use that supplies the wrong number of arguments, or
     names a definition that was never declared in that document, is a compile diagnostic (not a crash and
     not a silently absorbed `Unknown`), in the same family as the existing wrong-operand-count and
     unknown-predicate/operator diagnostics.
   - **Unused definitions.** A declared definition with no use anywhere in the document is a lint, opt-in
     alongside the other `LintRules` (ADR-0005's lint family), not an error and not on by default.
7. **Printing expands.** `CompiledRule.PrintRuleText`, `PrintJson` and `PrintYaml`, and `CanonicalText`,
   print the already-expanded tree: a rule that used definitions prints exactly as if every use had been
   written out by hand, because the tree holds no definitions to print. A printer that preserves the
   author's `let` statements and use sites is explicitly out of scope for this ADR and ticket 11; it is a
   later ticket, tracked separately. The owner may veto the "print expanded" default before that later
   ticket starts (see Open questions).
8. **Diagnostics inside an expansion name the definition.** A diagnostic span always points at the use
   site. When the problem is actually inside the definition's own body (for example, a term in the body
   itself fails a predicate-argument check, independent of which use triggered expansion), the diagnostic
   additionally names the definition and its own declaration span, so the author is pointed at the
   definition, not only at the use that happened to trigger checking it. This mirrors how the engine
   already reports a problem found through a rewrite or a builder-rendered tree at a path into the
   generated tree (ADR-0005 decision 11): a definition adds the definition's identity to that existing
   path/span reporting, it does not replace it.

## Open questions for the owner

Ticket 11-1 asks that anything ticket 11 or its parent leaves open be listed here rather than decided by
this ADR. The four questions below were resolved by the implementing ticket (11-2, 2026-10-16), as the
owner's standing instruction for this change directed; each resolution is recorded here rather than left
open, and amends this ADR in place.

- **Diagnostic codes (resolved 2026-10-16).** Allocated the next six free codes, `TRE0036`–`TRE0041`, in
  declaration order of decision 6's bullets: `TRE0036` `DefinitionCycleDetected`, `TRE0037`
  `DefinitionNameCollision`, `TRE0038` `MaxDefinitionDepthExceeded`, `TRE0039`
  `DefinitionArgumentCountMismatch`, `TRE0040` `UnknownDefinition`, `TRE0041` `UnusedDefinition`. "Expanded
  size counts against the normal limits" reuses the existing `TRE0009`/`TRE0010` as decision 6 already
  said, so it took no new code. Recorded in `src/TruthWeaver/Diagnostics/DiagnosticCodes.cs` and
  `docs/strong-k3/specification/diagnostics.md`.
- **`MaxDefinitionDepth`'s default value (resolved 2026-10-16).** Set to `8`. Rationale, also recorded on
  `CompilerOptions.MaxDefinitionDepth`'s XML documentation: it is deliberately small relative to
  `MaxDepth`'s 32, because a real reuse chain (a definition built from a couple of others, each built from
  a couple more) is rarely more than two or three links deep, so `8` catches a runaway or mistakenly
  self-referential chain long before it could threaten `MaxNodeCount` or `MaxDepth`, while leaving room for
  deliberate, deep reuse.
- **Source-preserving printing (resolved 2026-10-16, no change).** Stays out of scope for ticket 11, as
  decision 7 already said. The owner did not veto the expanded-printing default in this change; a
  source-preserving printer remains a separate, later ticket.
- **Exact JSON/YAML key names (resolved 2026-10-16, no change).** Confirmed as proposed:
  `definitions`/`use`/`parameters`/`body`, matching `rule-tree.schema.json`'s existing camelCase keys and
  lowercase `op` values. This ADR does not implement the JSON/YAML forms (ticket 11-3 does); the names are
  now the final API surface for that ticket to build against, not just a working name.

## Consequences

- The evaluator, BDD analyzer, rewrites (`ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`,
  `CompressToDerived`, `Canonicalize`, `Simplify`, the normal forms) and every printer need no change to
  support a rule that used definitions, because the tree they see has none: ticket 11's acceptance
  criterion "The BDD analysis, equivalence check, rewrites and every printer work on a rule that used
  definitions without change" follows directly from decision 1.
- `rule-tree.schema.json`, the JSON and YAML tree readers, the DSL grammar and lexer, and the
  `RuleBuilder` all need new code (tickets 11-2 through 11-4); this ADR does not change any of them
  itself.
- `CompilerOptions` gains one new member, `MaxDefinitionDepth`, alongside the existing resource bounds
  (`MaxDepth`, `MaxNodeCount`, `MaxAnalysisTerms`, `MaxRewriteNodeCount`); its default is an open question
  above.
- The diagnostic code catalog (`docs/strong-k3/specification/diagnostics.md`) and
  `DiagnosticCodes` gain new entries for the guardrails of decision 6, numbered by the implementing ticket
  (open question above).
- `CONTEXT.md` and `docs/glossary.md` gain entries for **definition** and **parameter** (added in this
  change, alongside this ADR, because the ADR introduces both as engine vocabulary).
- `let`, and the new JSON/YAML keys `definitions`/`use`/`parameters`/`body`, join the reserved-word and
  schema-key lists documented in `docs/rule-text.md` and `rule-tree.schema.json`; the implementing tickets
  update those pages in the same change that adds the words, per the project's docs-with-code rule.
- **Known limitation (recorded 2026-10-16, ticket 11-5).** Decision 8's "additionally names the definition" rule
  is fully implemented only for the definition guardrail diagnostics themselves (`TRE0036`-`TRE0040`, located at
  the use's own name). `TRE0009` `MaxDepthExceeded` and `TRE0010` `MaxNodeCountExceeded`, when the offending node
  came from inside a definition's own body (not from an argument substituted in at the use site, which already
  carries a real span at or near the use), still report only that node's span inside the `let` declaration, with
  no separate naming of the use site or the definition. Closing this fully would need `DefinitionExpander` to
  carry each expanded node's originating use site through every diagnostic `RuleNodeCompiler` can raise on an
  expanded node, not only these two, which ticket 11-5 judged larger than its reasonable scope; `docs/strong-k3/specification/diagnostics.md`
  records the same limitation precisely. No change to `Diagnostic`'s public shape is needed to close it later,
  because the extra identification can go in the message text, as the engine already does for other diagnostics'
  non-contractual detail.
