# System.Update

**Declarations**

```d4
create operator .System.Update(const ACursor : generic cursor, const ARow : generic row)
 class "System.CursorUpdateNode" static tags { DAE.IsFunctional = "False" };
```

**Remarks**

`Update` updates the current row of the specified cursor. It is an error to attempt to update a cursor that is positioned on the BOF or EOF cracks.

**Using System.Update**

```d4
begin
    var LCursor : cursor( table{ID: Integer, Name: String} );
    LCursor := cursor(Employee capabilities {Updateable});
    LCursor.Next();
    LCursor.Update(row{"Jim" Name});
    LCursor.Close();
end;
select Employee;
```

```text
Statement Executed Successfully!

ID Name
-- ----
1  Jim
2  John
3  Josh
4  Jeff
5  Jake
6  Jeff
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities) \| [Cursor Type](../../../developers-guide/d4-data-manipulation.md#cursor-type) \| [Data Manipulation](../../../developers-guide/d4-data-manipulation.md)
