# Derived Logical Operations

Logical connectives defined by composing the [gates](../gates/README.md). Each is a first-class operation with a canonical form in terms of primitives. Back to the [reference](../README.md); shared rules are in the [specification](../specification/README.md).

| Operation | Summary |
| --- | --- |
| [IMPLIES](implies.md) | Strong Kleene material implication, `NOT a OR b`; contrasted with Lukasiewicz |
| [EQUIVALENT](equivalent.md) | Biconditional, the negation of `XOR`; aliases `IFF` and `XNOR` |
| [XOR](xor.md) | Binary exclusive or; `Unknown` if either operand is |
| [NAND](nand.md) | Negated conjunction: the maximum of the negated operands |
| [NOR](nor.md) | Negated disjunction: the minimum of the negated operands |
| [PARITY](parity.md) | N-ary exclusive or; contrast with `ExactlyOne` in [cardinality](../cardinality/README.md) |
