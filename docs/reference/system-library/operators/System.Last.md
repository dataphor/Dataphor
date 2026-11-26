# System.Last

**Declarations**

```d4
create operator .System.Last(const ACursor : generic cursor)
 class "System.CursorLastNode";
```

**Remarks**

`Last` moves the specified cursor to the EOF crack just after the last row of the current dataset.

**Using System.Last**

```d4
begin
    var Output : String := "";
    var TempCursor : cursor(table { ID: Integer, Name : String});
    TempCursor := cursor(Employee capabilities { Updateable });
    TempCursor.Last();
    TempCursor.Prior();
    while not TempCursor.BOF() do
    begin
        Output := Output + String.Unicode({13})+ Name from TempCursor.Select();
        TempCursor.Prior();
    end;
    raise Error(Output + Unicode({13}));
end;
```

**See Also**

[Using Cursors](../../../developers-guide/modeling-process-logic.md#using-cursors) \| [Cursor Capabilities](../../../developers-guide/d4-data-manipulation.md#cursor-capabilities)
