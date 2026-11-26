# System.Open

**Declarations**

```d4
create operator .System.Open(const AExpression : System.String) : generic cursor
 class "System.SystemOpenNode" static tags { DAE.IsLiteral = "False", DAE.IsRemotable = "False" };
```

**Remarks**

`Open` opens a cursor based on a string representation of a table expression. The `Open` operator is similar to the Cursor selector, but this takes a string, rather than an expression allowing the user to construct a string at runtime for the cursor.

By default, the inferred characteristics for the `Open` operator are:

- non-literal
- functional
- deterministic
- repeatable
- nilable

**Using System.Open**

```d4
begin
    var EmployeeCursor : cursor(table { ID : Integer, Name : String });
    EmployeeCursor := Open("Employee") as cursor(table { ID : Integer, Name : String});
    try
        EmployeeCursor.Next();
        raise Error((Name from EmployeeCursor.Select()));
    finally
        EmployeeCursor.Close();
    end;
end;
```

```text
Joe
```

**See Also**

[System.Execute](System.Execute.md) \| [Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Dynamic Execution](../../../developers-guide/modeling-process-logic.md#dynamic-execution) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities)
