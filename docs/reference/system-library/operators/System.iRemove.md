# System.iRemove

**Declarations**

```d4
create operator .System.iRemove(const ARow : generic row) : generic row
 class "System.RowRemoveNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iRemove(ATable : generic table) : generic table
 class "System.RemoveNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`remove` is the parser recognized symbol for `iRemove`.

**See Also**

[Operator Precedence](../../../developers-guide/d4-language-elements.md#operator-precedence) \| [Over and Remove](../../../developers-guide/d4-table-operators.md#projection) \| [Table Operators](../../../developers-guide/d4-table-operators.md) \| [Using Rows](../../../developers-guide/modeling-process-logic.md#using-rows)

## System.iRemove (Row)

**Remarks**

`remove` is the parser recognized symbol for `iRemove`.

See [System.iRemove](System.iRemove.md).
