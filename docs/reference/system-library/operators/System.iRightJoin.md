# System.iRightJoin

**Declarations**

```d4
create operator .System.iRightJoin(ALeftTable : generic table, ARightTable : generic table) : generic table
 class "System.RightOuterJoinNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`right join` is the parser recognized symbol for `iRightJoin`.

**See Also**

[Join Operator](../../../developers-guide/d4-table-operators.md#join) \| [Outer Join Operator](../../../developers-guide/d4-table-operators.md#outer-join) \| [System.iJoin](System.iJoin.md) \| [System.iLeftJoin](System.iLeftJoin.md) \| [Select Statement](../../../developers-guide/d4-data-manipulation.md#select-statement) \| [Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
