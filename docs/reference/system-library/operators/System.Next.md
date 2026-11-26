# System.Next

**Declarations**

```d4
create operator .System.Next(const ACursor : generic cursor) : System.Boolean
 class "System.CursorNextNode";
```

**Remarks**

`Next` moves the specified cursor to the next row of the specified cursor. `next` returns true if the cursor is not on the EOF crack after the attempted move.

**Using System.Next**

```d4
begin
    var OutputString : String := Unicode({13}) + "From the Beginning: ";
    var EmployeeCursor : cursor(table { ID : Integer, Name : String }) := cursor(Employee);
    try
        EmployeeCursor.First();
        while EmployeeCursor.Next() do
        begin
            OutputString := OutputString + Unicode({13}) + (Name from EmployeeCursor.Select()) + " ";
        end;
        EmployeeCursor.Last();
        OutputString := OutputString + Unicode({13}) + "From the End: ";
        while EmployeeCursor.Prior() do
        begin
            OutputString := OutputString + Unicode({13}) + (Name from EmployeeCursor.Select()) + " ";
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

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursors Capabilities, Navigable](../../../developers-guide/d4-data-manipulation.md#navigable) \| [System.EOF](System.EOF.md)
