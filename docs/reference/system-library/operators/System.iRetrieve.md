# System.iRetrieve

**Declarations**

```d4
create operator .System.iRetrieve() : generic table
 class "System.BaseTableVarNode" static tags { DAE.IsBuiltin = "True" };
```

**Remarks**

Provides table variable access.

**Using System.iRetrieve**

```d4
Employees;
```

Stating a table implicitly calls the `iRetrieve` operator. This code returns the table named Employees, and could be used anywhere a table was called. This code will not output anything to the output window except for stating that it executed successfully. To output the contents of the table, the select operator will have to be used. For more information about the select statement, see its listing in the D4 Language guide.

**See Also**

[Select Statement](../../../developers-guide/d4-data-manipulation.md#select-statement)
