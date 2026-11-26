# System.iProject

**Declarations**

```d4
create operator .System.iProject(const ARow : generic row) : generic row
 class "System.RowProjectNode" static tags { DAE.IsBuiltin = "True" };
create operator .System.iProject(ATable : generic table) : generic table
 class "System.ProjectNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

`over` is the parser recognized symbol for `iProject`.

**See Also**

[Over and Remove](../../../developers-guide/d4-table-operators.md#projection) \| [Table Operators](../../../developers-guide/d4-table-operators.md) \| [Using Rows](../../../developers-guide/modeling-process-logic.md#using-rows)

## System.iProject (Row)

**Remarks**

`over` is the parser recognized symbol for `iProject`.

See [System.iProject](System.iProject.md).
