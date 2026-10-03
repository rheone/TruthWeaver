# Terminology

The terms the reference uses, each with one meaning. Back to the [specification index](README.md). The model is fixed in [PROPOSAL.md](../PROPOSAL.md) section 1 and [ADR-0005](../../adr/0005-strong-k3-language-surface.md).

Prose writes "Strong Kleene (K3)", never "K3" and "Strong K3" as two different systems ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 18).

## Classifying an Operation

| Term | Meaning |
| --- | --- |
| **Operation** | Umbrella term for anything with its own document: an operator, a cardinality function, a function, or a result transformation. Predicates become Operations once they are documented (on hold). |
| **Primary category** | The one grouping an Operation belongs to, and the directory its document lives in. Exactly one per Operation: Gates / Operators, Derived Logical Operations, Cardinality Functions, Functions, Result Transformations (and Predicates, on hold). Category is the topic; it is independent of Kind. |
| **Kind** | `Primitive` or `Derived`, stated explicitly in every Operation document. Independent of category: the cardinality and function categories hold both. |
| **Primitive** | An Operation with no definition in terms of other Operations. There are seven: `NOT`, `AND`, `OR`, `AtLeast`, `AtMost`, `Exactly` and `COALESCE`. `Collapse` is also recorded as Primitive, because nothing in the rule language expresses it. |
| **Derived** | An Operation that has a definition in terms of primitives. "Derived" means it is defined that way, not that the engine desugars it: a derived Operation stays a first-class node and keeps its own evaluation, printing and round-trip ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3a). |
| **Strong Kleene connective** | An Operation that is monotone in the [information order](values.md#information-order). See [semantics](semantics.md#strong-kleene-connectives-and-external-operators) for the list. |
| **External operator** | An Operation that is not monotone in the information order, so it is not a Strong Kleene connective: `COALESCE` and the inspections `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`. Flagged on every such document. |
| **Gate** | A label this reference uses for the primitive connectives `NOT`, `AND` and `OR`, which is the name of the Gates / Operators category. It is reference vocabulary only; the engine's own term is Operator ([CONTEXT.md](../../../CONTEXT.md)). |

## Forms of an Operation

An Operation can be written in several ways. The reference separates two senses of "form".

| Term | Meaning |
| --- | --- |
| **Canonical form** | For a derived Operation, its definition in terms of primitives, written as a function-call expression such as `OR(NOT(a), b)` for `IMPLIES`. A canonical form is listed only where one is established, and it is verified for every assignment of `T`, `F` and `U` and, for variadic Operations, every operand count and valid parameter. Where none is established the document says so. It is **not** the output of the engine's `Canonicalize()` rewrite (a reordering and flattening transform), nor the "canonical text" the printer writes. |
| **Public form** | The spelling a rule author or API consumer uses to name an Operation: the canonical printed word (`AND`, `AtLeast`, `If`), the JSON and YAML `op` value (`and`, `atLeast`), and the `RuleBuilder` member. Every accepted alternative spelling (a symbol such as `&&`, a word such as `IFF`) is an alias of the public form and compiles to the same node. Removed names such as `NXOR` are not aliases. The Syntax and Aliases sections of an Operation document list them. |

Canonical form is how an Operation is defined; public form is how it is named. A derived Operation keeps its public form on round-trip even though it has a canonical form.

## Evaluation terms

| Term | Meaning |
| --- | --- |
| **Truth order** | $\mathsf{F} < \mathsf{U} < \mathsf{T}$. An implementation aid; see [values](values.md#truth-order). |
| **Information order** | $\mathsf{U}$ below $\mathsf{T}$ and $\mathsf{F}$, which are incomparable. See [values](values.md#information-order). |
| **Strongest extension** | The three-valued function that is definite exactly when every classical resolution of the `Unknown` inputs agrees. See [semantics](semantics.md#truth-functional-evaluation-and-the-strongest-extension). |
| **Definitely true count, possibly true count** | For a cardinality Operation over $n$ operands, $d$ is the number of `True` operands and $p$ the number of `True` or `Unknown` operands. See [semantics](semantics.md#cardinality-uses-an-interval). |
| **Fault** | A predicate that failed to answer during one evaluation (an exception, a timeout or a cancellation). The expression sees `Unknown`; the fault is recorded on the decision. A predicate that returns `Unknown` records no fault. |

## Engine terms

| Term | Meaning |
| --- | --- |
| **Expression** | The immutable tree of operators over terms and constants. Every expression evaluates to exactly one value. |
| **Predicate** and **term** | A predicate is a registered, reusable function. A term is a predicate bound to concrete arguments, and is a leaf of the tree. The two are not interchangeable. |
| **Decision** | The result of evaluating an expression: a value plus any faults, and optionally a trace. |
| **Result transformation** | A method on a `Decision`, outside the rule language: `Decision.Project(unknownAs)` replaces `Unknown` with a chosen definite value, and `Decision.Collapse(policy)` turns the value into a `CollapseOutcome`. `Project` and `Collapse` are TruthWeaver terms, not Strong Kleene (K3) terms; in relational algebra "projection" means selecting columns. Both are pure and never record a fault. |
| **Rewrite** | An opt-in, value-preserving transform of a compiled rule that returns a new rule: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize` and `Simplify`. |

## Related

- [values](values.md), [semantics](semantics.md) and [notation](notation.md).
- [CONTEXT.md](../../../CONTEXT.md) holds the engine's full vocabulary.
