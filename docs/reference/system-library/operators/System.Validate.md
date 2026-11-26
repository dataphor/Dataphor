# System.Validate

**Declarations**

```d4
create operator .System.Validate(const ACursor : generic cursor, const AOldRow : generic row, var ANewRow : generic row) : System.Boolean
 class "System.CursorValidateNode" static tags { DAE.IsFunctional = "False" };
create operator .System.Validate(const ACursor : generic cursor, const AOldRow : generic row, var ANewRow : generic row, const AColumnName : System.String) : System.Boolean
 class "System.CursorValidateNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`Validate` is a cursor level operator. It allows access to the proposable calls for the cursor. It permits a "what if" check.

By passing in the proposed row in `ARow`, `Validate` verifies that the appropriate constraints have been met.

If `AColumnName` is specified, only that column is involved in the proposal. If `AColumnName` is omitted, all columns are involved.

The boolean value returned is `true` if a change occured to any of the columns in the row, `false` if no change occured. Note that `true` would be returned only if a validation handler were implemented.

**Using System.Validate**

```text
See System.Change for the example.
```

**See Also**

[System.Change](System.Change.md) \| [System.Validate](System.Validate.md)
