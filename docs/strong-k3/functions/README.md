# Functions

Value operations that are not plain connectives. `COALESCE` and the four inspections are external operators, not Strong Kleene connectives; `If` is a connective. Back to the [reference](../README.md).

| Operation | Summary |
| --- | --- |
| [COALESCE](coalesce.md) | First operand that is not `Unknown`; alias `??`; external operator |
| [If](if.md) | Conditional; an `Unknown` condition gives a value only when both branches agree; Strong Kleene connective |
| [IsTrue](istrue.md) | Inspection: `True` when the operand is `True`, otherwise `False`; external operator |
| [IsFalse](isfalse.md) | Inspection: `True` when the operand is `False`, otherwise `False`; external operator |
| [IsUnknown](isunknown.md) | Inspection: `True` when the operand is `Unknown`, otherwise `False`; external operator |
| [IsKnown](isknown.md) | Inspection: `True` unless the operand is `Unknown`; external operator |
