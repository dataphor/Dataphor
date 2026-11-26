# System.Prior

**Declarations**

```d4
create operator .System.Prior(const ACursor : generic cursor) : System.Boolean
 class "System.CursorPriorNode";
```

**Remarks**

<sup>Prior</sup> moves the specified cursor to the prior (or previous) row of the specified cursor. `prior` returns true if the cursor is not on the BOF crack after the attempted move.

**Using System.Prior**

```d4
begin
    var OutputString : String := Unicode({13}) + "From the Beginning: ";
    var EmployeeCursor : cursor(table { ID : Integer, Name : String }) := cursor(Employee);
    try
        EmployeeCursor.First();
        while EmployeeCursor.Next() do
        begin
            OutputString :=
    OutputString
    + Unicode({13})
    + (Name from EmployeeCursor.Select())
    + " ";
        end;
        EmployeeCursor.Last();
        OutputString :=
   OutputString
   + Unicode({13})
   + "From the End: ";
        while EmployeeCursor.Prior() do
        begin
            OutputString :=
    OutputString
    + Unicode({13})
    + (Name from EmployeeCursor.Select())
    + " ";
        end;
        raise Error(OutputString);
    finally
        EmployeeCursor.Close();
    end;
end;
```

```text
From the Beginning: 
Joe 
John 
Josh 
Jeff 
Jake 
Jeff 
From the End: 
Jeff 
Jake 
Jeff 
Josh 
John 
Joe
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [System.BOF](System.BOF.md)
