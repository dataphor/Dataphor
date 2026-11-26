# System.Refresh

**Declarations**

```d4
create operator .System.Refresh(const ACursor : generic cursor, const ARow : generic row)
 class "System.CursorRefreshNode";
create operator .System.Refresh(const ACursor : generic cursor)
 class "System.CursorRefreshNode";
```

**Remarks**

`Refresh` refreshes the current cursor. `Refresh` closes, reopens, and does a `FindNearest` on the cursor. If a row is not specified as the current location, the current row is used in the `FindNearest`. Note that regardless of whether the cursor was declared static or dynamic, calling refresh will always re-evaluate the cursor expression against the database.

**Using System.Refresh**

```d4
begin
    var EmployeeCursor :
  cursor( table { ID : Integer, Name : String });
    EmployeeCursor := cursor(Employee);
    var NewRow : row { ID : Integer, Name : String };
    NewRow := row { 9 ID, "Jack" Name};
    insert table { NewRow } into Employee;
    EmployeeCursor.Refresh(NewRow);
    raise Error
 (
  "The current row is: "
  + (Name from EmployeeCursor.Select() )
 );
end;
```

```text
The current row is: Jack
```

**See Also**

[System.Reset](System.Reset.md) \| [Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [Cursor Type](../../../developers-guide/d4-data-manipulation.md#cursor-type) \| [System.FindNearest](System.FindNearest.md)
