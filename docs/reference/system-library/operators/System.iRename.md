# System.iRename

**Declarations**

```d4
create operator .System.iRename(const ARow : generic row) : generic row
 class "System.RowRenameNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iRename(ATable : generic table) : generic table
 class "System.RenameNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`rename` is the parser recognized symbol for `iRename`.

**See Also**

[Rename](../../../developers-guide/d4-table-operators.md#rename) \| [Table Operators](../../../developers-guide/d4-table-operators.md)

## System.iRename (Row)

**Remarks**

`rename` is the parser recognized symbol for `iRename`.

See [System.iRename](System.iRename.md).
