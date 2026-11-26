# System.iJoin

**Declarations**

```d4
create operator .System.iJoin(const ALeftRow : generic row, const ARightRow : generic row) : generic row
 class "System.RowJoinNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iJoin(ALeftTable : generic table, ARightTable : generic table) : generic table
 class "System.JoinNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`join`, `lookup`, `intersect`, and `times` are the parser recognized symbols for `iJoin`. This operator combines two rows or tables together.

**See Also**

[Join](../../../developers-guide/d4-table-operators.md#join) \| [Outer Join](../../../developers-guide/d4-table-operators.md#outer-join) \| [System.iLeftJoin](System.iLeftJoin.md) \| [System.iRightJoin](System.iRightJoin.md) \| [Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Intersect - Table Operator](../../../developers-guide/d4-table-operators.md#intersect) \| [Times - Table Operator](../../../developers-guide/d4-table-operators.md#times) \| [Using Rows](../../../developers-guide/modeling-process-logic.md#using-rows)

## System.iJoin (Row)

**Remarks**

See [System.iJoin](System.iJoin.md)
