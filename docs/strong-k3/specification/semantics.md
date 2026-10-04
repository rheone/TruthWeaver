# Semantics

How the Strong Kleene (K3) connectives are defined, how a compound expression is evaluated, and which classical laws survive. Back to the [specification index](README.md). Values and orders are in [values](values.md); symbols in [notation](notation.md).

Every table, canonical form and law below holds for every assignment of `T`, `F` and `U` to its variables.

## The three primitive connectives

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$:

$$\neg a = \text{the value reversed: } \mathsf{T} \leftrightarrow \mathsf{F},\ \mathsf{U} \mapsto \mathsf{U} \qquad a \land b = \min(a, b) \qquad a \lor b = \max(a, b)$$

<!-- k3:truth NOT -->
| a | NOT(a) |
| --- | --- |
| T | F |
| U | U |
| F | T |

<!-- k3:truth AND -->
| a | b | AND(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | U |
| T | F | F |
| U | T | U |
| U | U | U |
| U | F | F |
| F | T | F |
| F | U | F |
| F | F | F |

<!-- k3:truth OR -->
| a | b | OR(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | T |
| T | F | T |
| U | T | T |
| U | U | U |
| U | F | U |
| F | T | T |
| F | U | U |
| F | F | F |

A definite operand can settle the result without the other: `F` settles `AND` and `T` settles `OR`. `U` settles neither, so `U` is not a short-circuit value. `AND` and `OR` extend to any number of operands by taking the minimum or maximum of all of them.

Every other connective is defined from these three, or from the cardinality interval below.

## Truth-functional evaluation and the strongest extension

TruthWeaver evaluates **truth-functionally**: the value of a node depends only on the values of its operands, and each occurrence of a sub-expression is evaluated on its own. Two occurrences of the same term are not tied together.

The **strongest extension** of a Boolean function $g$ is the three-valued function $g^{*}$ that answers a definite value exactly when every classical resolution of the `Unknown` inputs agrees:

$$g^{*}(x) = \begin{cases} b & \text{if } g(y) = b \text{ for every } y \text{ obtained from } x \text{ by replacing each } \mathsf{U} \text{ by } \mathsf{T} \text{ or } \mathsf{F} \\ \mathsf{U} & \text{otherwise} \end{cases}$$

For each Strong Kleene connective the operation equals the strongest extension of its own Boolean restriction: `AND`, `OR`, `NOT`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, the cardinality operations and `If` all do.

The two notions part ways on **compound expressions that repeat a variable**. Evaluating `a OR NOT a` combines the value of `a` with the value of `NOT a` as if they were independent, so with `a` unknown it gets `U`, although every classical resolution gives `T`:

| a | NOT(a) | OR(a, NOT(a)) | Every resolution of a gives |
| --- | --- | --- | --- |
| T | F | T | T |
| U | U | U | T |
| F | T | T | T |

The same gap explains each failure listed under [laws that fail](#laws-that-fail). It is not a defect: it is what keeps evaluation compositional and linear in the size of the rule. No rewrite closes the gap by replacing `a OR NOT a` with `True`.

### Cardinality uses an interval

For operands $x_1, \dots, x_n$ let $d$ be the number of definite `True` operands and $p$ the number that are `True` or `Unknown`, so $d \le p \le n$ and the true count lies in the interval $[d, p]$. A cardinality operation answers `True` when every count in the interval satisfies its condition, `False` when none does, and `Unknown` otherwise. This is the strongest extension of the Boolean counting predicate. For `AtLeast(2)` over three operands:

<!-- k3:eval AtLeast n=3 k=2 -->
| Definitely true (d) | Possibly true (p) | AtLeast(2, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | F |
| 1 | 2 | U |
| 1 | 3 | U |
| 2 | 2 | T |
| 2 | 3 | T |
| 3 | 3 | T |

## Laws that hold

The following hold for every assignment of $\mathsf{T}$, $\mathsf{F}$, $\mathsf{U}$ to $a$, $b$ and $c$. Each holds for all 27 assignments. De Morgan's laws are also given below as canonical forms.

| Law | Statement |
| --- | --- |
| Commutativity | $a \land b = b \land a$, $\quad a \lor b = b \lor a$ |
| Associativity | $a \land (b \land c) = (a \land b) \land c$, $\quad a \lor (b \lor c) = (a \lor b) \lor c$ |
| Idempotence | $a \land a = a$, $\quad a \lor a = a$ |
| Distributivity | $a \land (b \lor c) = (a \land b) \lor (a \land c)$, $\quad a \lor (b \land c) = (a \lor b) \land (a \lor c)$ |
| Absorption | $a \lor (a \land b) = a$, $\quad a \land (a \lor b) = a$ |
| Identity | $a \land \mathsf{T} = a$, $\quad a \lor \mathsf{F} = a$ |
| Annihilation | $a \land \mathsf{F} = \mathsf{F}$, $\quad a \lor \mathsf{T} = \mathsf{T}$ |
| Double negation | $\neg\neg a = a$ |
| De Morgan | $\neg(a \land b) = \neg a \lor \neg b$, $\quad \neg(a \lor b) = \neg a \land \neg b$ |
| Contraposition | $a \to b = \neg b \to \neg a$ |
| Biconditional | $a \leftrightarrow b = (a \to b) \land (b \to a) = \neg(a \oplus b)$ |

De Morgan's laws, as canonical forms of the Operations they define:

<!-- k3:canonical NAND vars=a,b -->
```text
OR(NOT(a), NOT(b))
```

<!-- k3:canonical NOR vars=a,b -->
```text
AND(NOT(a), NOT(b))
```

<!-- k3:canonical AND vars=a,b -->
```text
NOT(OR(NOT(a), NOT(b)))
```

<!-- k3:canonical OR vars=a,b -->
```text
NOT(AND(NOT(a), NOT(b)))
```

Together with double negation these make `NOT`, `AND` and `OR` a De Morgan (Kleene) algebra. All three are also monotone in the [information order](values.md#information-order).

## Laws that fail

Every classical law that needs a variable and its negation to cancel fails, because `U` and `NOT(U)` are both `U`. Each row fails at $a = \mathsf{U}$ only; both definite values satisfy it.

| Classical law | a = T | a = U | a = F | Fails because |
| --- | --- | --- | --- | --- |
| Excluded middle, $a \lor \neg a = \mathsf{T}$ | T | **U** | T | `OR(U, U)` is `U` |
| Non-contradiction, $a \land \neg a = \mathsf{F}$ | F | **U** | F | `AND(U, U)` is `U` |
| Identity, $a \to a = \mathsf{T}$ | T | **U** | T | `IMPLIES(U, U)` is `U` |
| Reflexive equivalence, $a \leftrightarrow a = \mathsf{T}$ | T | **U** | T | `EQUIVALENT(U, U)` is `U` |
| Self-exclusion, $a \oplus a = \mathsf{F}$ | F | **U** | F | `XOR(U, U)` is `U` |
| Complementary exclusion, $a \oplus \neg a = \mathsf{T}$ | T | **U** | T | `XOR(U, U)` is `U` |

The identity row also shows that `IMPLIES` is Kleene's strong implication, not Lukasiewicz's, where `U -> U` is `T`:

<!-- k3:truth IMPLIES -->
| a | b | IMPLIES(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | U |
| T | F | F |
| U | T | T |
| U | U | U |
| U | F | U |
| F | T | T |
| F | U | T |
| F | F | T |

Distributivity of `AND` over `XOR` fails as well: with $a = \mathsf{U}$ and $b = c = \mathsf{T}$, $a \land (b \oplus c)$ is `AND(U, F)`, which is `F`, while $(a \land b) \oplus (a \land c)$ is `XOR(U, U)`, which is `U`.

### No tautologies, no contradictions

An expression built only from variables and the Strong Kleene connectives (no `True`, `False` or `Unknown` constants) evaluates to `Unknown` when every variable is `Unknown`. So such an expression is never `True` for every assignment (a tautology) and never `False` for every assignment (a contradiction). The argument is by induction: every connective maps all-`Unknown` operands to `Unknown`. The analyzer relies on this: it does not report `a OR NOT a` as a tautology.

The complement laws hold for definite values only, which is why a rewrite may apply them to constants but not to variables.

### The invalid consensus removal

In classical logic $(a \land b) \lor (\neg a \land c) \lor (b \land c) = (a \land b) \lor (\neg a \land c)$: the consensus term $b \land c$ is redundant. In Strong Kleene (K3) it is not. Take $a = \mathsf{U}$ and $b = c = \mathsf{T}$:

| Expression | Value at a = U, b = T, c = T |
| --- | --- |
| $(a \land b) \lor (\neg a \land c) \lor (b \land c)$ | `T`, because the consensus term is `T` |
| $(a \land b) \lor (\neg a \land c)$ | `U`, because both remaining terms are `U` |

This is the only one of the 27 assignments where the two differ. It matters because `If(c, t, f)` is defined **with** the consensus term, which makes it the strongest extension of the classical conditional. Removing the term is invalid, and no TruthWeaver rewrite does it:

<!-- k3:canonical If vars=c,t,f -->
```text
OR(AND(c, t), AND(NOT(c), f), AND(t, f))
```

## Strong Kleene connectives and external operators

A **Strong Kleene connective** is monotone in the [information order](values.md#information-order): refining an `Unknown` operand never changes a definite result. These are `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, `PARITY`, the cardinality operations (`AtLeast`, `AtMost`, `Exactly`, `ExactlyOne`, `GreaterThan`, `LessThan`, `ANY`, `ALL`, `NONE`, `BETWEEN`) and `If`.

An **external operator** can observe `Unknown` and answer something definite because of it, so it is not monotone. These are `COALESCE` and the four inspections `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`. Their precedents are SQL (`COALESCE`, `IS [NOT] TRUE/FALSE/UNKNOWN`) and Bochvar's external connectives. Each Operation's document states which kind it is.

| | Strong Kleene connective | External operator |
| --- | --- | --- |
| Monotone in the information order | Yes | No |
| Equals the strongest extension of its Boolean restriction | Yes | No |
| Covered by the no-tautology theorem | Yes | No |
| Can be rewritten using `NAND` alone or `NOR` alone | Yes | No |

The language as a whole is Strong Kleene (K3) plus external operators. Two consequences follow.

The no-tautology theorem does not extend to them. `IsKnown(a) OR IsUnknown(a)` is a tautology, because exactly one of the two is `T` for every `a`:

<!-- k3:truth IsKnown -->
| a | IsKnown(a) |
| --- | --- |
| T | T |
| U | F |
| F | T |

<!-- k3:truth IsUnknown -->
| a | IsUnknown(a) |
| --- | --- |
| T | F |
| U | T |
| F | F |

`NAND` and `NOR` cannot express them. Every circuit built from `NOT`, `AND`, `OR`, `NAND` and `NOR` is monotone, and `COALESCE` is not (see the example in [values](values.md#information-order)).

`Project` and `Collapse` are neither connectives nor external rule operators: they are methods on an evaluated decision, described in the [result transformations](../result-transformations/README.md).

## Related

- [values](values.md), [terminology](terminology.md) and [notation](notation.md) complete the specification.
- [operations](operations.md) lists the Operations that apply these definitions.
