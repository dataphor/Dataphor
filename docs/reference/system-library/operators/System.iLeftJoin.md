# System.iLeftJoin

**Declarations**

```d4
create operator .System.iLeftJoin(ALeftTable : generic table, ARightTable : generic table) : generic table
 class "System.LeftOuterJoinNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`left join` is the parser recognized symbol for `iLeftJoin`.

**See Also**

[Join Operator](../../../developers-guide/d4-table-operators.md#join) \| [Outer Join Operator](../../../developers-guide/d4-table-operators.md#outer-join) \| [System.iJoin](System.iJoin.md) \| [System.iRightJoin](System.iRightJoin.md) \| [Select Statement](../../../developers-guide/d4-data-manipulation.md#select-statement) \| [Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence)
