# System.Default

**Declarations**

```d4
create operator .System.Default(const ACursor : generic cursor, var ARow : generic row) : System.Boolean
 class "System.CursorDefaultNode" static tags { DAE.IsFunctional = "False" };
create operator .System.Default(const ACursor : generic cursor, var ARow : generic row, const AColumnName : System.String) : System.Boolean
 class "System.CursorDefaultNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`Default` is a cursor level operator. It allows access to the proposable calls for the cursor. It permits a "what if" check.

By passing in the proposed new row in `ARow`, the results that would occur if the insert were made are passed back in `ARow`. These changes are due to the defaults assigned to columns and scalar types involved in the expression for the cursor, as well as default event handlers.

If `AColumnName` is specified, only that column is involved in the proposal. If `AColumnName` is omitted, all columns are involved.

The boolean value returned is `true` if a change occured to any of the columns in the row, `false` if no change occured.

**Using System.Default**

```text
See System.Change for the example.
```

**See Also**

[System.Change](System.Change.md) \| [System.Validate](System.Validate.md)
