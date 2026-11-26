# System.Reset

**Declarations**

```d4
create operator .System.Reset(const ACursor : generic cursor)
 class "System.CursorResetNode";
```

**Remarks**

`Reset` resets the current cursor. `Reset` closes and reopens the cursor. Note that regardless of whether the cursor was declared static or dynamic, calling refresh will always re-evaluate the cursor expression against the database.

**Using System.Reset**

```d4
begin
    var EmployeeCursor : cursor( table { ID : Integer, Name : String });
    EmployeeCursor := cursor(Employee);
    var NewRow : row { ID : Integer, Name : String };   NewRow := row { 11 ID, "Jose" Name};    insert table { NewRow } into Employee;
    EmployeeCursor.Reset();
    EmployeeCursor.FindKey(row { 11 ID } );
    raise Error("The current row is: " + (Name from EmployeeCursor.Select() ) );
end;
```

```text
The current row is: Jose
```

**See Also**

[System.Refresh](System.Refresh.md) \| [Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [Cursor Type](../../../developers-guide/d4-data-manipulation.md#cursor-type)
