# System.iExtend

**Declarations**

```d4
create operator .System.iExtend(const ARow : generic row) : generic row
 class "System.RowExtendNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iExtend(ATable : generic table) : generic table
 class "System.ExtendNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`add` is the parser recognized symbol for `iExtend`.

**See Also**

[Table Expressions, Add](../../../developers-guide/d4-table-operators.md#extension) \| [Table Expressions](../../../developers-guide/d4-table-operators.md)

## System.iExtend (Row)

**Remarks**

`add` is the parser recognized symbol for `iExtend`. `iExtend` is a table and a row operator.

**See**

[System.iExtend table operator](System.iExtend.md)
