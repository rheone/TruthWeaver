# Cardinality Functions

Operations over the count of true operands. `AtLeast`, `AtMost` and `Exactly` are primitive; the rest are derived from them. Back to the [reference](../README.md).

| Operation | Kind | Summary |
| --- | --- | --- |
| [AtLeast](atleast.md) | Primitive | At least `k` operands are true; `AtLeast(1)` is `OR`, `AtLeast(n)` is `AND` |
| [AtMost](atmost.md) | Primitive | At most `k` operands are true; the negation of `AtLeast(k + 1)` |
| [Exactly](exactly.md) | Primitive | Exactly `k` operands are true; `AND(AtLeast(k), AtMost(k))` |
| [ExactlyOne](exactlyone.md) | Derived | Exactly one operand is true; contrast with `PARITY` |
| [GreaterThan](greaterthan.md) | Derived | More than `k` operands are true; `AtLeast(k + 1)` |
| [LessThan](lessthan.md) | Derived | Fewer than `k` operands are true; `AtMost(k - 1)` |
| ANY | Derived | At least one operand is true (pending) |
| ALL | Derived | Every operand is true (pending) |
| NONE | Derived | No operand is true (pending) |
| BETWEEN | Derived | The true count lies within `min` and `max` (pending) |
