# System.Delete

**Declarations**

```d4
create operator .System.Delete(const ACursor : generic cursor)
 class "System.CursorDeleteNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`Delete` deletes the current row from the specified cursor.

**Using System.Delete**

```d4
begin
    BeginTransaction();
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.First();
    TempCursor.Next();
    TempCursor.Insert( row { 16 ID, "Jed" Name});
    TempCursor.Delete();
    PrepareTransaction();
    RollbackTransaction();
end;
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors)
